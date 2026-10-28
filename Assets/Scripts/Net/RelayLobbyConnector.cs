using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using MythMayhem.Gameplay;
using MythMayhem.UI;

namespace MythMayhem.Net
{
    // Owns the runtime NetworkManager and the UGS Relay handshake. The created
    // Relay join code doubles as the lobby code (no separate Lobby service).
    //
    // The host is the only peer allowed to change scenes, so it also drives the
    // automatic lobby -> loading screen -> battle transition. Every step is
    // guarded: connection timeouts, scene event retries, load watchdogs and
    // peer disconnects all fall back to a usable lobby (or the main menu) while
    // reporting the reason to the player instead of silently stalling.
    public class RelayLobbyConnector : MonoBehaviour, ILobbyConnector
    {
        public const string LobbySceneName = "PvPLobby";
        public const string BattleSceneName = "PlayWithAI";
        public const string LoadingSceneName = "LoadingScreen";
        public const string MainMenuSceneName = "MainMenu";

        // How long a joining client waits for the host to acknowledge the handshake.
        private const float ClientConnectTimeoutSeconds = 20f;
        // How long the relay/auth handshake may take before it is considered hung.
        private const float LobbyHandshakeTimeoutSeconds = 30f;
        // Fade-out is 0.45s; give it a moment before unloading the lobby scene.
        private const float FadeOutDelaySeconds = 0.5f;
        // Backstop for a scene event that never reports completion.
        private const float SceneLoadTimeoutSeconds = 40f;
        private const float SceneEventRetryDelaySeconds = 0.25f;
        private const int SceneEventRetryAttempts = 20;
        private const float LoadingScreenSettleSeconds = 0.75f;
        private const float ReturnToMenuDelaySeconds = 2.5f;

        public static RelayLobbyConnector Instance { get; private set; }

        // True from Shutdown() until the next session starts. Gameplay systems
        // use it to tell an intentional exit from an unexpected disconnect.
        public static bool IsShuttingDown { get; private set; }

        public event Action<LobbyStatus> StateChanged;

        public string JoinCode { get; private set; }
        public LobbyStatus LastStatus { get; private set; } = new LobbyStatus(LobbyConnectionState.Idle, string.Empty);

        private NetworkManager networkManager;
        private UnityTransport transport;
        private Coroutine clientConnectRoutine;
        private Coroutine handshakeRoutine;
        private bool shuttingDown;
        private bool clientConnected;
        private bool pendingHostSceneLoad;
        private bool transitioning;
        private bool sceneLoadCompleted;
        private string pendingSceneLoadName;
        private string sceneLoadError;
        private int sessionGeneration;
        private readonly List<ulong> timedOutClients = new List<ulong>();

        // Registers the factory at startup so UI code (which has no netcode
        // references) can spin up the connector on demand.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterFactory()
        {
            LobbyConnectorLocator.Factory = () => Ensure();
        }

        public static RelayLobbyConnector Ensure()
        {
            if (Instance != null)
                return Instance;

            GameObject host = new GameObject("NetworkServices");
            DontDestroyOnLoad(host);

            // Transport first so the NetworkManager never exists without one.
            host.AddComponent<UnityTransport>();
            host.AddComponent<NetworkManager>();
            RelayLobbyConnector connector = host.AddComponent<RelayLobbyConnector>();

            Instance = connector;
            LobbyConnectorLocator.Current = connector;
            return connector;
        }

        private void Awake()
        {
            Instance = this;
            LobbyConnectorLocator.Current = this;

            networkManager = GetComponent<NetworkManager>();
            if (networkManager == null)
                networkManager = gameObject.AddComponent<NetworkManager>();

            transport = GetComponent<UnityTransport>();
            if (transport == null)
                transport = gameObject.AddComponent<UnityTransport>();

            if (networkManager.NetworkConfig == null)
                networkManager.NetworkConfig = new NetworkConfig();

            NetworkConfig config = networkManager.NetworkConfig;
            config.NetworkTransport = transport;
            config.EnableSceneManagement = true;
            // A mismatched build used to fail deep inside NGO's scene hash lookup.
            // Reject it during approval instead so the player gets a clear message.
            config.ConnectionApproval = true;
            config.ClientConnectionBufferTimeout = 15;
            config.LoadSceneTimeOut = 30;

            networkManager.ConnectionApprovalCallback = HandleConnectionApproval;
            networkManager.OnClientConnectedCallback += HandleClientConnected;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            networkManager.OnTransportFailure += HandleTransportFailure;
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= HandleClientConnected;
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                networkManager.OnTransportFailure -= HandleTransportFailure;
                networkManager.ConnectionApprovalCallback = null;
                UnsubscribeSceneEvents();
            }

            if (Instance == this)
                Instance = null;

            if (ReferenceEquals(LobbyConnectorLocator.Current, this))
                LobbyConnectorLocator.Current = null;
        }

        private void Update()
        {
            if (!pendingHostSceneLoad || transitioning)
                return;

            if (networkManager == null || !networkManager.IsServer || !networkManager.IsListening)
                return;

            // Wait until the remote peer is fully synchronized before the host
            // opens the synchronized loading screen.
            if (!HasRemotePeer())
                return;

            pendingHostSceneLoad = false;
            transitioning = true;
            StartCoroutine(RunMatchStartSequence());
        }

        // --- Public lobby API ---

        public async Task<string> CreateLobbyAsync(int maxConnections = 1)
        {
            int generation = await PrepareSessionAsync();
            if (!IsSessionCurrent(generation))
                return null;

            ReportStatus(LobbyConnectionState.Creating, "Creating lobby…");
            StartHandshakeTimeout("Creating the lobby timed out. Check the connection and try again.");

            try
            {
                await InitializeServicesAsync();
                if (!IsSessionCurrent(generation))
                    return null;

                // Set the seat before any scene spawns so MatchSession reads the right side.
                NetworkSession.Mode = NetworkGameMode.HostPvp;

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, maxConnections));
                if (!IsSessionCurrent(generation))
                    return null;

                JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                if (!IsSessionCurrent(generation))
                    return null;

                transport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    true);

                if (!networkManager.StartHost())
                    throw new Exception("The host could not start. Check the device's network connection.");

                SubscribeSceneEvents();
                ReportStatus(LobbyConnectionState.WaitingForOpponent,
                    "Share the code, then wait for your opponent to join.");
                return JoinCode;
            }
            catch (Exception e)
            {
                Debug.LogError("[PvP] Create lobby failed: " + e);
                Shutdown();
                throw;
            }
        }

        public async Task JoinLobbyAsync(string joinCode)
        {
            string code = (joinCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("Enter the lobby code first.");

            int generation = await PrepareSessionAsync();
            if (!IsSessionCurrent(generation))
                return;

            ReportStatus(LobbyConnectionState.Joining, "Joining lobby…");
            StartHandshakeTimeout("Joining the lobby timed out. Check the code and the connection, then try again.");

            try
            {
                await InitializeServicesAsync();
                if (!IsSessionCurrent(generation))
                    return;

                // Set the seat before the synced scene spawns.
                NetworkSession.Mode = NetworkGameMode.ClientPvp;

                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(code);
                if (!IsSessionCurrent(generation))
                    return;

                transport.SetClientRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    allocation.HostConnectionData,
                    true);

                if (!networkManager.StartClient())
                    throw new Exception("Could not start the connection. Check the device's network connection.");

                SubscribeSceneEvents();
                ReportStatus(LobbyConnectionState.Joining, "Connecting to host…");
                StartClientConnectTimeout();
            }
            catch (Exception e)
            {
                Debug.LogError("[PvP] Join lobby failed: " + e);
                Shutdown();
                throw;
            }
        }

        // Host-only restart hook used by MatchSession's "play again" flow.
        public void LoadNetworkMatch()
        {
            if (networkManager == null || !networkManager.IsServer || networkManager.SceneManager == null)
            {
                Debug.LogError("[PvP] Cannot restart because network scene management is unavailable.");
                ReturnToMenuAfterFailedRestart();
                return;
            }

            if (transitioning)
                return;

            if (!HasRemotePeer())
            {
                Debug.LogWarning("[PvP] Cannot restart without an opponent.");
                ReturnToMenuAfterFailedRestart();
                return;
            }

            transitioning = true;
            StartCoroutine(RunMatchStartSequence());
        }

        // A restart that cannot happen must not strand the player on the result
        // screen with no working button.
        private void ReturnToMenuAfterFailedRestart()
        {
            Shutdown();
            SceneTransitionManager.LoadScene(MainMenuSceneName);
        }

        public void Shutdown()
        {
            if (shuttingDown)
                return;

            shuttingDown = true;
            IsShuttingDown = true;
            sessionGeneration++;

            StopConnectTimeout();
            StopHandshakeTimeout();
            StopAllCoroutines();

            pendingHostSceneLoad = false;
            transitioning = false;
            sceneLoadCompleted = false;
            pendingSceneLoadName = null;
            sceneLoadError = null;
            clientConnected = false;
            JoinCode = null;
            timedOutClients.Clear();

            UnsubscribeSceneEvents();

            if (networkManager != null && (networkManager.IsServer || networkManager.IsClient || networkManager.IsListening))
                networkManager.Shutdown();

            NetworkSession.Reset();
            LastStatus = new LobbyStatus(LobbyConnectionState.Idle, string.Empty);
        }

        // --- Connection lifecycle ---

        private async Task<int> PrepareSessionAsync()
        {
            ValidateScenesAvailable();

            if (networkManager == null)
                throw new InvalidOperationException("Networking is unavailable in this build.");

            // Invalidate any handshake still in flight before touching the session.
            sessionGeneration++;
            int generation = sessionGeneration;

            if (networkManager.IsListening)
            {
                if (!networkManager.ShutdownInProgress)
                    networkManager.Shutdown(true);
                await WaitForNetworkShutdownAsync();

                // The player left (or a newer attempt started) while we waited;
                // leave the state alone for whichever attempt now owns it.
                if (generation != sessionGeneration)
                    return generation;

                if (networkManager.IsListening)
                    throw new InvalidOperationException("The previous session is still closing. Try again in a moment.");
            }

            shuttingDown = false;
            IsShuttingDown = false;
            clientConnected = false;
            pendingHostSceneLoad = false;
            transitioning = false;
            sceneLoadCompleted = false;
            pendingSceneLoadName = null;
            sceneLoadError = null;
            timedOutClients.Clear();
            NetworkSession.Reset();

            networkManager.NetworkConfig.ConnectionData = BitConverter.GetBytes(GetBuildSignature());
            return generation;
        }

        // False when the caller was cancelled (left the lobby) while an await was
        // in flight, or when a newer session attempt superseded it.
        private bool IsSessionCurrent(int generation)
        {
            return !shuttingDown && generation == sessionGeneration && networkManager != null;
        }

        private async Task WaitForNetworkShutdownAsync()
        {
            // NGO finishes its shutdown on the next network update, which fires
            // the disconnect callbacks we want to ignore before a new attempt.
            for (int frame = 0; frame < 60; frame++)
            {
                if (networkManager == null || !networkManager.IsListening)
                    return;

                await Task.Yield();
            }
        }

        private static async Task InitializeServicesAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (networkManager == null)
                return;

            if (networkManager.IsServer)
            {
                // Ignore the host's own local client id.
                if (clientId == networkManager.LocalClientId)
                    return;

                pendingHostSceneLoad = true;
                ReportStatus(LobbyConnectionState.Connected, "Opponent connected. Preparing the match…");
                return;
            }

            if (clientId != networkManager.LocalClientId)
                return;

            clientConnected = true;
            StopConnectTimeout();
            ReportStatus(LobbyConnectionState.Connected, "Connected. Waiting for the host to start the match…");
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (shuttingDown || networkManager == null)
                return;

            bool inLobby = IsInScene(LobbySceneName);
            bool onLoadingScreen = IsInScene(LoadingSceneName);

            if (networkManager.IsServer)
            {
                if (clientId == networkManager.LocalClientId)
                    return;
            }
            else
            {
                clientConnected = false;
            }

            // Mid-match departures are turned into a win/loss overlay by MatchSession.
            if (!inLobby && !onLoadingScreen)
                return;

            string reason = networkManager.DisconnectReason;
            string message = string.IsNullOrEmpty(reason)
                ? (networkManager.IsServer ? "Your opponent left the lobby." : "The host ended the session.")
                : "Disconnected: " + reason;

            AbortSession(message);
        }

        private void HandleTransportFailure()
        {
            if (shuttingDown)
                return;

            // NGO is already tearing the session down. During a match MatchSession
            // reports the result; everywhere else we explain what happened.
            if (IsInScene(BattleSceneName))
                return;

            string reason = networkManager != null ? networkManager.DisconnectReason : null;
            AbortSession(string.IsNullOrEmpty(reason)
                ? "The connection was lost. Check both devices are online and try again."
                : "The connection was lost: " + reason);
        }

        // --- Match start sequence (host only) ---

        private IEnumerator RunMatchStartSequence()
        {
            ReportStatus(LobbyConnectionState.StartingMatch, "Starting the match…");
            SceneTransitionManager.FadeOutForNetworkSceneLoad();
            yield return new WaitForSecondsRealtime(FadeOutDelaySeconds);

            if (!HasRemotePeer())
            {
                AbortMatchStart("Your opponent disconnected before the match started.");
                yield break;
            }

            yield return LoadNetworkSceneRoutine(LoadingSceneName);
            if (!string.IsNullOrEmpty(sceneLoadError))
            {
                AbortMatchStart(sceneLoadError);
                yield break;
            }

            yield return new WaitForSecondsRealtime(LoadingScreenSettleSeconds);

            if (!HasRemotePeer())
            {
                AbortMatchStart("Your opponent disconnected before the match started.");
                yield break;
            }

            yield return LoadNetworkSceneRoutine(BattleSceneName);
            if (!string.IsNullOrEmpty(sceneLoadError))
            {
                AbortMatchStart(sceneLoadError);
                yield break;
            }

            // The battle scene is live and MatchSession takes over from here.
            transitioning = false;
            pendingHostSceneLoad = false;
        }

        private IEnumerator LoadNetworkSceneRoutine(string sceneName)
        {
            sceneLoadError = null;
            sceneLoadCompleted = false;
            pendingSceneLoadName = sceneName;
            timedOutClients.Clear();

            if (networkManager == null || networkManager.SceneManager == null)
            {
                sceneLoadError = "Network scene management is unavailable.";
                yield break;
            }

            SceneEventProgressStatus status = SceneEventProgressStatus.None;
            bool loadRequested = false;

            for (int attempt = 0; attempt < SceneEventRetryAttempts; attempt++)
            {
                if (!HasRemotePeer())
                {
                    sceneLoadError = "Your opponent disconnected while the match was loading.";
                    yield break;
                }

                try
                {
                    status = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    sceneLoadError = "The match could not be prepared. Please try again.";
                    yield break;
                }

                if (status == SceneEventProgressStatus.Started)
                {
                    loadRequested = true;
                    break;
                }

                // Another scene event is still finishing; retry shortly.
                if (status != SceneEventProgressStatus.SceneEventInProgress)
                    break;

                yield return new WaitForSecondsRealtime(SceneEventRetryDelaySeconds);
            }

            if (!loadRequested)
            {
                sceneLoadError = DescribeSceneLoadFailure(sceneName, status);
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + SceneLoadTimeoutSeconds;
            while (!sceneLoadCompleted)
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    sceneLoadError = "Timed out while loading the " + DescribeScene(sceneName) + ".";
                    yield break;
                }

                if (!HasRemotePeer())
                {
                    sceneLoadError = "Your opponent disconnected while the match was loading.";
                    yield break;
                }

                yield return null;
            }

            if (timedOutClients.Count > 0)
            {
                sceneLoadError = "Your opponent's game did not finish loading the " + DescribeScene(sceneName)
                    + ". Both players must run the same build of the game.";
                yield break;
            }
        }

        private void HandleLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (networkManager == null || !networkManager.IsServer)
                return;

            if (!string.Equals(sceneName, pendingSceneLoadName, StringComparison.Ordinal))
                return;

            timedOutClients.Clear();
            if (clientsTimedOut != null)
                timedOutClients.AddRange(clientsTimedOut);

            sceneLoadCompleted = true;
        }

        private void HandleSceneEvent(SceneEvent sceneEvent)
        {
            if (networkManager == null || networkManager.IsServer)
                return;

            bool isMatchTransition = string.Equals(sceneEvent.SceneName, LoadingSceneName, StringComparison.Ordinal)
                || string.Equals(sceneEvent.SceneName, BattleSceneName, StringComparison.Ordinal);

            // Clients follow the host; fade out as soon as a match scene load
            // starts. The initial lobby sync must not blank the screen.
            if (isMatchTransition && sceneEvent.SceneEventType == SceneEventType.Load)
                SceneTransitionManager.FadeOutForNetworkSceneLoad();
        }

        private void AbortMatchStart(string message)
        {
            Debug.LogWarning("[PvP] Match start aborted: " + message);

            bool stayInLobby = IsInScene(LobbySceneName);
            bool onLoadingScreen = IsInScene(LoadingSceneName);

            transitioning = false;
            pendingHostSceneLoad = false;
            sceneLoadCompleted = false;
            pendingSceneLoadName = null;
            sceneLoadError = null;

            Shutdown();
            ReportStatus(LobbyConnectionState.Failed, message);
            SceneTransitionManager.CancelNetworkFade();

            if (stayInLobby)
                return;

            if (onLoadingScreen)
            {
                LoadingScreenController.SetStatusMessage(message + " Returning to the main menu…");
                StartCoroutine(ReturnToMainMenuRoutine());
                return;
            }

            // The peer vanished after the battle scene became active but before
            // the match started; leave instead of stranding a dead board.
            SceneTransitionManager.LoadScene(MainMenuSceneName);
        }

        private void AbortSession(string message)
        {
            // Never kill a live match from here: MatchSession owns the result overlay.
            if (IsInScene(BattleSceneName))
                return;

            Debug.LogWarning("[PvP] " + message);

            bool onLoadingScreen = IsInScene(LoadingSceneName);
            bool inLobby = IsInScene(LobbySceneName);

            Shutdown();
            ReportStatus(LobbyConnectionState.Failed, message);

            if (inLobby)
                SceneTransitionManager.CancelNetworkFade();

            if (!onLoadingScreen)
                return;

            SceneTransitionManager.CancelNetworkFade();
            LoadingScreenController.SetStatusMessage(message + " Returning to the main menu…");
            StartCoroutine(ReturnToMainMenuRoutine());
        }

        private IEnumerator ReturnToMainMenuRoutine()
        {
            yield return new WaitForSecondsRealtime(ReturnToMenuDelaySeconds);
            SceneTransitionManager.LoadScene(MainMenuSceneName);
        }

        private void StartHandshakeTimeout(string failureMessage)
        {
            StopHandshakeTimeout();
            handshakeRoutine = StartCoroutine(HandshakeTimeoutRoutine(failureMessage));
        }

        // Covers the Unity Services / Relay allocation phase, which has no
        // timeout of its own. Once the transport starts listening the handshake
        // itself is covered by the client connect timeout.
        private IEnumerator HandshakeTimeoutRoutine(string failureMessage)
        {
            float deadline = Time.realtimeSinceStartup + LobbyHandshakeTimeoutSeconds;
            while (!shuttingDown && Time.realtimeSinceStartup < deadline)
            {
                if (networkManager == null || networkManager.IsListening)
                    break;

                yield return null;
            }

            handshakeRoutine = null;

            if (shuttingDown || networkManager == null || networkManager.IsListening)
                yield break;

            AbortSession(failureMessage);
        }

        private void StopHandshakeTimeout()
        {
            if (handshakeRoutine == null)
                return;

            StopCoroutine(handshakeRoutine);
            handshakeRoutine = null;
        }

        private void StartClientConnectTimeout()
        {
            StopConnectTimeout();
            clientConnectRoutine = StartCoroutine(ClientConnectTimeoutRoutine());
        }

        private IEnumerator ClientConnectTimeoutRoutine()
        {
            float deadline = Time.realtimeSinceStartup + ClientConnectTimeoutSeconds;
            while (!clientConnected && !shuttingDown && Time.realtimeSinceStartup < deadline)
            {
                // NGO abandons an unanswered handshake on its own (buffer
                // timeout). Surface that immediately instead of waiting out our
                // longer, friendlier timeout.
                if (networkManager == null || !networkManager.IsListening)
                    break;

                yield return null;
            }

            clientConnectRoutine = null;

            if (clientConnected || shuttingDown)
                yield break;

            AbortSession("Could not reach the host. Check the code, and make sure both devices are online.");
        }

        private void StopConnectTimeout()
        {
            if (clientConnectRoutine == null)
                return;

            StopCoroutine(clientConnectRoutine);
            clientConnectRoutine = null;
        }

        private void SubscribeSceneEvents()
        {
            if (networkManager == null || networkManager.SceneManager == null)
                return;

            networkManager.SceneManager.OnSceneEvent -= HandleSceneEvent;
            networkManager.SceneManager.OnSceneEvent += HandleSceneEvent;
            networkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
            networkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
        }

        private void UnsubscribeSceneEvents()
        {
            if (networkManager == null || networkManager.SceneManager == null)
                return;

            networkManager.SceneManager.OnSceneEvent -= HandleSceneEvent;
            networkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        }

        // --- Connection approval (build parity) ---

        private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            response.PlayerPrefabHash = null;
            response.Pending = false;

            // The host approves its own local client; remote ids are never 0.
            if (networkManager != null && request.ClientNetworkId == networkManager.LocalClientId)
            {
                response.Approved = true;
                return;
            }

            if (request.Payload == null || request.Payload.Length < sizeof(uint))
            {
                response.Approved = false;
                response.Reason = "Build mismatch. Both players must install the same version of the game.";
                return;
            }

            uint remoteSignature = BitConverter.ToUInt32(request.Payload, 0);
            if (remoteSignature != GetBuildSignature())
            {
                response.Approved = false;
                response.Reason = "Build mismatch. Both players must install the same version of the game.";
                return;
            }

            response.Approved = true;
        }

        private static uint buildSignature;
        private static bool buildSignatureComputed;

        // Hash of the enabled scene list. Two builds that disagree here cannot
        // synchronize scenes, so we reject the connection before it gets that far.
        private static uint GetBuildSignature()
        {
            if (buildSignatureComputed)
                return buildSignature;

            uint hash = 2166136261u;
            int sceneCount = SceneManager.sceneCountInBuildSettings;

            for (int i = 0; i < sceneCount; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(path))
                    continue;

                for (int c = 0; c < path.Length; c++)
                {
                    hash ^= path[c];
                    hash *= 16777619u;
                }

                hash ^= 0x2Fu;
                hash *= 16777619u;
            }

            buildSignature = hash == 0u ? 1u : hash;
            buildSignatureComputed = true;
            return buildSignature;
        }

        // --- Helpers ---

        private bool HasRemotePeer()
        {
            return networkManager != null
                && networkManager.IsServer
                && networkManager.IsListening
                && networkManager.ConnectedClientsIds.Count >= 2;
        }

        private static bool IsInScene(string sceneName)
        {
            Scene active = SceneManager.GetActiveScene();
            return active.IsValid() && string.Equals(active.name, sceneName, StringComparison.Ordinal);
        }

        private static void ValidateScenesAvailable()
        {
            RequireScene(LobbySceneName);
            RequireScene(LoadingSceneName);
            RequireScene(BattleSceneName);
        }

        private static void RequireScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
                throw new InvalidOperationException("Scene '" + sceneName + "' is missing from Build Settings.");
        }

        private static string DescribeScene(string sceneName)
        {
            if (sceneName == LoadingSceneName)
                return "loading screen";

            if (sceneName == BattleSceneName)
                return "battle scene";

            return sceneName + " scene";
        }

        private static string DescribeSceneLoadFailure(string sceneName, SceneEventProgressStatus status)
        {
            switch (status)
            {
                case SceneEventProgressStatus.SceneEventInProgress:
                    return "The match could not start because another scene was still loading. Please try again.";
                case SceneEventProgressStatus.InvalidSceneName:
                case SceneEventProgressStatus.SceneFailedVerification:
                    return "The " + DescribeScene(sceneName) + " is missing from Build Settings.";
                case SceneEventProgressStatus.SceneManagementNotEnabled:
                    return "Network scene management is disabled.";
                default:
                    return "The " + DescribeScene(sceneName) + " could not be loaded (" + status + ").";
            }
        }

        private void ReportStatus(LobbyConnectionState state, string message)
        {
            LastStatus = new LobbyStatus(state, message);

            Action<LobbyStatus> handler = StateChanged;
            if (handler == null)
                return;

            try
            {
                handler(LastStatus);
            }
            catch (Exception e)
            {
                // A broken listener must never take the session down with it.
                Debug.LogException(e);
            }
        }
    }
}
