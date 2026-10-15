using System;
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

namespace MythMayhem.Net
{
    // Owns the runtime NetworkManager and the UGS Relay handshake. The created
    // Relay join code doubles as the lobby code (no separate Lobby service).
    public class RelayLobbyConnector : MonoBehaviour, ILobbyConnector
    {
        public const string BattleSceneName = "SampleScene";

        public static RelayLobbyConnector Instance { get; private set; }

        private NetworkManager networkManager;
        private UnityTransport transport;
        private bool pendingHostSceneLoad;

        public string JoinCode { get; private set; }

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
            host.AddComponent<RelayLobbyConnector>();
            return Instance;
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

            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.EnableSceneManagement = true;
            networkManager.OnClientConnectedCallback += HandleClientConnected;
        }

        private void OnDestroy()
        {
            if (networkManager != null)
                networkManager.OnClientConnectedCallback -= HandleClientConnected;

            if (Instance == this)
                Instance = null;

            if (LobbyConnectorLocator.Current == this)
                LobbyConnectorLocator.Current = null;
        }

        private void Update()
        {
            if (!pendingHostSceneLoad)
                return;

            if (networkManager == null || !networkManager.IsServer)
                return;

            // Wait until the remote peer actually finished connecting before the
            // host swaps to the battle scene; the client follows via scene sync.
            if (networkManager.ConnectedClientsIds.Count < 2)
                return;

            pendingHostSceneLoad = false;
            networkManager.SceneManager.LoadScene(BattleSceneName, LoadSceneMode.Single);
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (networkManager != null && networkManager.IsServer && clientId != NetworkManager.ServerClientId)
                pendingHostSceneLoad = true;
        }

        private static async Task InitializeServicesAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Creates the relay allocation, starts the host and returns the join code.
        // The host stays in the lobby until a peer connects (see Update).
        public async Task<string> CreateLobbyAsync(int maxConnections = 1)
        {
            await InitializeServicesAsync();

            // Set the seat before any scene spawns so MatchSession reads the right side.
            NetworkSession.Mode = NetworkGameMode.HostPvp;

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                true);

            if (!networkManager.StartHost())
                throw new Exception("Failed to start Relay host.");

            return JoinCode;
        }

        public async Task JoinLobbyAsync(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
                throw new ArgumentException("Join code is empty.");

            await InitializeServicesAsync();

            // Set the seat before the synced scene spawns.
            NetworkSession.Mode = NetworkGameMode.ClientPvp;

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim().ToUpperInvariant());

            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData,
                true);

            if (!networkManager.StartClient())
                throw new Exception("Failed to start Relay client.");
        }

        public void Shutdown()
        {
            pendingHostSceneLoad = false;
            JoinCode = null;

            if (networkManager != null && (networkManager.IsServer || networkManager.IsClient))
                networkManager.Shutdown();

            NetworkSession.Reset();
        }
    }
}
