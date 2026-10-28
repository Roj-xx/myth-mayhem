using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using MythMayhem.Audio;
using MythMayhem.Board;
using MythMayhem.Gameplay;
using MythMayhem.UI;

namespace MythMayhem.Net
{
    // Host-authoritative match controller. The host owns the board, turn and hero
    // health; the joining client sends intents and renders replicated snapshots.
    // Hands/decks stay local to each peer (fixed decks, demo scope).
    [RequireComponent(typeof(Unity.Netcode.NetworkObject))]
    public class MatchSession : NetworkBehaviour, IMatchIntentSink
    {
        // NetworkManager.ServerClientId is 0; kept as a literal to avoid clashing
        // with the inherited instance property named "NetworkManager".
        private const ulong ServerClientId = 0UL;

        [SerializeField] private TurnManager turnManager;
        [SerializeField] private MatchController matchController;
        [SerializeField] private CardPlayController cardPlay;
        [SerializeField] private CombatController combat;
        [SerializeField] private BoardManager board;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private DeckController playerDeck;
        [SerializeField] private DeckController opponentDeck;
        [SerializeField] private BoardView boardView;

        private CardDefinitionLookup lookup;
        private CreatureSide lastActiveSide;
        private int revision;
        private bool endingTurn;
        private NetworkManager subscribedManager;

        public override void OnNetworkSpawn()
        {
            lookup = new CardDefinitionLookup();
            lookup.Build(playerDeck, opponentDeck);

            NetworkSession.Intents = this;
            lastActiveSide = turnManager != null ? turnManager.ActiveSide : CreatureSide.Player;

            SubscribeDisconnectEvents();

            // A peer can vanish in the gap between the scene finishing its load
            // and this object spawning, in which case the disconnect callback is
            // already gone. Do not strand the remaining player on a dead board.
            if (NetworkSession.Mode != NetworkGameMode.SinglePlayerAI && !HasRemotePeer())
            {
                Debug.LogWarning("[PvP] The opponent is gone; returning to the main menu.");
                NetworkSession.SessionLost = true;
                RelayLobbyConnector.Instance?.Shutdown();
                SceneTransitionManager.LoadScene(RelayLobbyConnector.MainMenuSceneName);
                return;
            }

            if (IsServer)
            {
                StartCoroutine(BroadcastAfterFrame());
            }
            else
            {
                FlipClientCamera();
                RequestSnapshotServerRpc();
            }
        }

        public override void OnNetworkDespawn()
        {
            UnsubscribeDisconnectEvents();

            if (ReferenceEquals(NetworkSession.Intents, this))
                NetworkSession.Intents = null;
        }

        private IEnumerator BroadcastAfterFrame()
        {
            // Wait one frame so TurnManager.Start has finished its initial setup.
            yield return null;
            BroadcastSnapshot();
        }

        private void Update()
        {
            if (!NetworkSession.IsNetworked || turnManager == null)
                return;

            CreatureSide active = turnManager.ActiveSide;
            if (active == lastActiveSide)
                return;

            lastActiveSide = active;

            // Each peer draws its own card when its turn begins.
            if (active == NetworkSession.LocalSide && playerDeck != null)
                playerDeck.DrawToHand();
        }

        // --- IMatchIntentSink (called by the local player's controllers) ---

        public bool PlayCreature(string cardName, int lane)
        {
            if (IsServer)
                return ApplyCreature(NetworkSession.LocalSide, cardName, lane);

            PlayCreatureServerRpc(cardName, lane);
            return true;
        }

        public bool PlayBuilding(string cardName, int lane)
        {
            if (IsServer)
                return ApplyBuilding(NetworkSession.LocalSide, cardName, lane);

            PlayBuildingServerRpc(cardName, lane);
            return true;
        }

        public bool PlaySupport(string cardName, int lane)
        {
            if (IsServer)
                return ApplySupport(NetworkSession.LocalSide, cardName, lane);

            PlaySupportServerRpc(cardName, lane);
            return true;
        }

        public void RequestEndTurn()
        {
            if (IsServer)
                ApplyEndTurn(NetworkSession.LocalSide);
            else
                RequestEndTurnServerRpc();
        }

        public void Concede()
        {
            if (IsServer)
                ApplyConcede(NetworkSession.LocalSide);
            else
                ConcedeServerRpc();
        }

        public void RestartMatch()
        {
            if (IsServer)
                LoadMatchScene();
            else
                RestartMatchServerRpc();
        }

        // --- Server RPCs (client -> host) ---

        [ServerRpc(RequireOwnership = false)]
        private void PlayCreatureServerRpc(string cardName, int lane, ServerRpcParams rpcParams = default)
        {
            ApplyCreature(SideOf(rpcParams.Receive.SenderClientId), cardName, lane);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlayBuildingServerRpc(string cardName, int lane, ServerRpcParams rpcParams = default)
        {
            ApplyBuilding(SideOf(rpcParams.Receive.SenderClientId), cardName, lane);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlaySupportServerRpc(string cardName, int lane, ServerRpcParams rpcParams = default)
        {
            ApplySupport(SideOf(rpcParams.Receive.SenderClientId), cardName, lane);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestEndTurnServerRpc(ServerRpcParams rpcParams = default)
        {
            ApplyEndTurn(SideOf(rpcParams.Receive.SenderClientId));
        }

        [ServerRpc(RequireOwnership = false)]
        private void ConcedeServerRpc(ServerRpcParams rpcParams = default)
        {
            ApplyConcede(SideOf(rpcParams.Receive.SenderClientId));
        }

        [ServerRpc(RequireOwnership = false)]
        private void RestartMatchServerRpc()
        {
            LoadMatchScene();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestSnapshotServerRpc(ServerRpcParams rpcParams = default)
        {
            SendSnapshot(rpcParams.Receive.SenderClientId);
        }

        // --- Host-authoritative rule application ---

        private bool ApplyCreature(CreatureSide side, string cardName, int lane)
        {
            if (!CanAct(side))
                return false;

            CreatureDefinition definition = lookup.Creature(cardName);
            if (definition == null || !turnManager.CanAfford(side, definition.Cost))
                return false;

            if (!cardPlay.IsLaneOpenForCreature(side, lane))
                return false;

            string failure;
            CreatureInstance summoned = cardPlay.TrySummonCreature(side, definition, lane, out failure);
            if (summoned == null)
                return false;

            turnManager.Spend(side, definition.Cost);
            BroadcastSnapshot();
            return true;
        }

        private bool ApplyBuilding(CreatureSide side, string cardName, int lane)
        {
            if (!CanAct(side))
                return false;

            BuildingDefinition definition = lookup.Building(cardName);
            if (definition == null || !turnManager.CanAfford(side, definition.Cost))
                return false;

            if (!cardPlay.IsLaneOpenForBuilding(side, lane))
                return false;

            if (!cardPlay.TryPlaceBuilding(side, definition, lane))
                return false;

            turnManager.Spend(side, definition.Cost);
            BroadcastSnapshot();
            return true;
        }

        private bool ApplySupport(CreatureSide side, string cardName, int lane)
        {
            if (!CanAct(side))
                return false;

            SupportCardDefinition support = lookup.Support(cardName);
            if (support == null || !turnManager.CanAfford(side, support.Cost))
                return false;

            CreatureSide targetSide = support.EffectType == SupportEffectType.EnemyAttackDebuff ? Other(side) : side;
            CreatureInstance target = cardPlay.GetCreature(targetSide, lane);
            if (target == null)
                return false;

            if (!cardPlay.TryApplySupport(side, support, target))
                return false;

            turnManager.Spend(side, support.Cost);
            PlaySupportSoundClientRpc();
            BroadcastSnapshot();
            return true;
        }

        [ClientRpc]
        private void PlaySupportSoundClientRpc()
        {
            AudioManager.PlaySpawnSound();
        }

        private void ApplyEndTurn(CreatureSide side)
        {
            if (endingTurn)
                return;

            if (turnManager == null || matchController == null || matchController.IsMatchOver)
                return;

            if (turnManager.ActiveSide != side)
                return;

            endingTurn = true;
            StartCoroutine(ApplyEndTurnRoutine(side));
        }

        // Resolves combat lane by lane, then advances the turn and replicates the result.
        private IEnumerator ApplyEndTurnRoutine(CreatureSide side)
        {
            try
            {
                if (combat != null)
                    yield return combat.ResolveCombatRoundRoutine();

                if (matchController.IsMatchOver)
                {
                    BroadcastSnapshot();
                    yield break;
                }

                CreatureSide next = Other(side);
                turnManager.NetworkAdvanceTurn(next);
                BroadcastSnapshot();
            }
            finally
            {
                endingTurn = false;
            }
        }

        // Conceding is host-authoritative: the sender's side loses, and the
        // resulting snapshot carries the outcome to both peers.
        private void ApplyConcede(CreatureSide side)
        {
            if (matchController == null || matchController.IsMatchOver)
                return;

            MatchOutcome result = side == CreatureSide.Player
                ? MatchOutcome.OpponentWin
                : MatchOutcome.PlayerWin;

            matchController.EndMatch(result);
            BroadcastSnapshot();
        }

        private bool CanAct(CreatureSide side)
        {
            if (matchController == null || matchController.IsMatchOver)
                return false;

            return turnManager != null && turnManager.ActiveSide == side;
        }

        private CreatureSide SideOf(ulong clientId)
        {
            return clientId == ServerClientId ? CreatureSide.Player : CreatureSide.Opponent;
        }

        private static CreatureSide Other(CreatureSide side)
        {
            return side == CreatureSide.Player ? CreatureSide.Opponent : CreatureSide.Player;
        }

        private void LoadMatchScene()
        {
            if (matchController == null || !matchController.IsMatchOver)
                return;

            if (NetworkManager == null || NetworkManager.SceneManager == null)
            {
                Debug.LogError("[PvP] Cannot restart because network scene management is unavailable.");
                return;
            }

            RelayLobbyConnector connector = RelayLobbyConnector.Instance;
            if (connector == null)
            {
                Debug.LogError("[PvP] Cannot restart because the Relay lobby connector is unavailable.");
                return;
            }

            connector.LoadNetworkMatch();
        }

        // --- Snapshot replication ---

        private byte[] BuildSnapshotBytes()
        {
            BoardSnapshot snapshot = BoardSnapshot.CreateEmpty(BoardManager.LaneCount);
            snapshot.Revision = ++revision;
            snapshot.ActiveSide = (int)turnManager.ActiveSide;
            snapshot.PlayerPoints = turnManager.GetPoints(CreatureSide.Player);
            snapshot.OpponentPoints = turnManager.GetPoints(CreatureSide.Opponent);
            snapshot.PlayerHeroHp = matchController.PlayerHealth;
            snapshot.OpponentHeroHp = matchController.OpponentHealth;
            snapshot.Outcome = (int)matchController.Outcome;

            for (int sideIndex = 0; sideIndex < BoardSnapshot.SideCount; sideIndex++)
            {
                CreatureSide side = (CreatureSide)sideIndex;

                for (int lane = 0; lane < BoardManager.LaneCount; lane++)
                {
                    int index = BoardSnapshot.Index(side, lane, BoardManager.LaneCount);

                    CreatureInstance creature = cardPlay.GetCreature(side, lane);
                    if (creature != null && creature.IsAlive)
                    {
                        snapshot.Creatures[index] = new CreatureNetState
                        {
                            Present = true,
                            CardName = creature.Definition != null ? creature.Definition.DisplayName : string.Empty,
                            Health = creature.CurrentHealth,
                            AttackMod = creature.AttackModifier,
                            ModTurns = creature.AttackModifierTurns,
                        };
                    }

                    BuildingInstance building = cardPlay.GetBuilding(side, lane);
                    if (building != null && building.IsAlive)
                    {
                        snapshot.Buildings[index] = new BuildingNetState
                        {
                            Present = true,
                            CardName = building.Definition != null ? building.Definition.DisplayName : string.Empty,
                            Health = building.CurrentHealth,
                        };
                    }
                }
            }

            return BoardSnapshotCodec.Encode(snapshot, BoardManager.LaneCount);
        }

        private void BroadcastSnapshot()
        {
            SyncBoardClientRpc(BuildSnapshotBytes());
        }

        private void SendSnapshot(ulong clientId)
        {
            ClientRpcParams parameters = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId },
                },
            };

            SyncBoardClientRpc(BuildSnapshotBytes(), parameters);
        }

        [ClientRpc]
        private void SyncBoardClientRpc(byte[] data, ClientRpcParams rpcParams = default)
        {
            if (data == null)
                return;

            BoardSnapshot snapshot = BoardSnapshotCodec.Decode(data);
            ApplySnapshot(snapshot);
        }

        private void ApplySnapshot(BoardSnapshot snapshot)
        {
            CreatureSide active = (CreatureSide)snapshot.ActiveSide;

            turnManager.NetworkApplyState(active, snapshot.PlayerPoints, snapshot.OpponentPoints);
            matchController.SetHealth(snapshot.PlayerHeroHp, snapshot.OpponentHeroHp);
            boardView.Apply(snapshot, lookup);

            MatchOutcome outcome = (MatchOutcome)snapshot.Outcome;
            if (outcome != MatchOutcome.None)
                matchController.NetworkApplyOutcome(outcome);
        }

        // --- Connection loss handling ---

        private bool HasRemotePeer()
        {
            NetworkManager manager = NetworkManager;
            if (manager == null)
                return false;

            if (IsServer)
                return manager.IsListening && manager.ConnectedClientsIds.Count >= 2;

            return manager.IsConnectedClient;
        }

        private void SubscribeDisconnectEvents()
        {
            NetworkManager manager = NetworkManager;
            if (manager == null)
                return;

            subscribedManager = manager;
            manager.OnClientDisconnectCallback += HandleClientDisconnected;
            manager.OnTransportFailure += HandleTransportFailure;
        }

        private void UnsubscribeDisconnectEvents()
        {
            if (subscribedManager == null)
                return;

            subscribedManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            subscribedManager.OnTransportFailure -= HandleTransportFailure;
            subscribedManager = null;
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (subscribedManager == null || RelayLobbyConnector.IsShuttingDown)
                return;

            if (!ReferenceEquals(NetworkSession.Intents, this))
                return;

            // The host is told which client left; a client is told about its own
            // id when the host goes away. Ignore the host's own shutdown notice.
            bool opponentLeft = IsServer && clientId != subscribedManager.LocalClientId;
            bool hostLeft = !IsServer && clientId == subscribedManager.LocalClientId;
            if (!opponentLeft && !hostLeft)
                return;

            AwardWalkover(opponentLeft ? "Your opponent left the match." : "The host left the match.");
        }

        private void HandleTransportFailure()
        {
            if (RelayLobbyConnector.IsShuttingDown)
                return;

            if (!ReferenceEquals(NetworkSession.Intents, this))
                return;

            AwardWalkover("The connection was lost.");
        }

        // A mid-match disconnect ends the session, so award the remaining player
        // the win instead of leaving them on a frozen board.
        private void AwardWalkover(string message)
        {
            // The peer is gone either way, so a restart is no longer possible.
            NetworkSession.SessionLost = true;

            if (matchController == null || matchController.IsMatchOver)
                return;

            Debug.LogWarning("[PvP] " + message + " Awarding the match to the local player.");
            matchController.EndMatch(MatchOutcome.PlayerWin);
        }

        // --- Client view setup ---

        private void FlipClientCamera()
        {
            Camera camera = mainCamera != null ? mainCamera : Camera.main;
            if (camera == null || board == null)
                return;

            Transform playerSpawn = board.GetPlayerSpawn(1);
            Transform opponentSpawn = board.GetOpponentSpawn(1);
            if (playerSpawn == null || opponentSpawn == null)
                return;

            Vector3 center = (playerSpawn.position + opponentSpawn.position) * 0.5f;
            camera.transform.RotateAround(center, Vector3.up, 180f);
        }
    }
}
