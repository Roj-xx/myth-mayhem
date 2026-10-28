using System;
using System.Threading.Tasks;

namespace MythMayhem.Gameplay
{
    // High level state of an online session. The lobby UI renders this as-is so
    // players always know whether they are connecting, connected or failed.
    public enum LobbyConnectionState
    {
        Idle,
        Creating,
        Joining,
        WaitingForOpponent,
        Connected,
        StartingMatch,
        Failed
    }

    public readonly struct LobbyStatus
    {
        public LobbyStatus(LobbyConnectionState state, string message)
        {
            State = state;
            Message = message ?? string.Empty;
        }

        public LobbyConnectionState State { get; }
        public string Message { get; }

        // Busy states lock the create/join controls; Idle and Failed release them.
        public bool IsBusy => State != LobbyConnectionState.Idle && State != LobbyConnectionState.Failed;
    }

    // Abstraction over the Relay lobby handshake so UI code can drive matchmaking
    // without referencing the networking packages directly.
    public interface ILobbyConnector
    {
        event Action<LobbyStatus> StateChanged;
        LobbyStatus LastStatus { get; }
        Task<string> CreateLobbyAsync(int maxConnections = 1);
        Task JoinLobbyAsync(string joinCode);
        void Shutdown();
    }

    // Lets the networking layer register how to create a connector at runtime.
    // Kept netcode-free so menus/single-player builds never touch NGO types.
    public static class LobbyConnectorLocator
    {
        public static ILobbyConnector Current;
        public static Func<ILobbyConnector> Factory;

        public static bool Available => Current != null || Factory != null;

        public static ILobbyConnector Ensure()
        {
            if (Current != null)
                return Current;

            if (Factory == null)
                return null;

            return Factory();
        }
    }
}
