using System;
using System.Threading.Tasks;

namespace MythMayhem.Gameplay
{
    // Abstraction over the Relay lobby handshake so UI code can drive matchmaking
    // without referencing the networking packages directly.
    public interface ILobbyConnector
    {
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
