namespace MythMayhem.Gameplay
{
    public enum NetworkGameMode
    {
        SinglePlayerAI,
        HostPvp,
        ClientPvp
    }

    // Requests the local player wants to perform. In networked play the active
    // MatchSession implements this and routes the request to the host. In
    // single-player it stays null and the controllers mutate the game directly.
    public interface IMatchIntentSink
    {
        bool PlayCreature(string cardName, int lane);
        bool PlayBuilding(string cardName, int lane);
        bool PlaySupport(string cardName, int lane);
        void RequestEndTurn();
        void RestartMatch();
    }

    // Lightweight global describing the current session. Kept free of any
    // netcode references so the gameplay scripts can branch on it without
    // dragging the networking packages into every assembly.
    public static class NetworkSession
    {
        public static NetworkGameMode Mode = NetworkGameMode.SinglePlayerAI;
        public static IMatchIntentSink Intents;

        public static bool IsNetworked => Mode != NetworkGameMode.SinglePlayerAI;
        public static bool IsHost => Mode == NetworkGameMode.HostPvp;
        public static bool IsClient => Mode == NetworkGameMode.ClientPvp;

        // Seat model: the host plays the Player side (bottom of the board);
        // the joining client plays the Opponent side (top) and has its camera
        // flipped so its own side is still rendered along the bottom.
        public static CreatureSide LocalSide =>
            Mode == NetworkGameMode.ClientPvp ? CreatureSide.Opponent : CreatureSide.Player;

        public static CreatureSide RemoteSide =>
            LocalSide == CreatureSide.Player ? CreatureSide.Opponent : CreatureSide.Player;

        public static void Reset()
        {
            Mode = NetworkGameMode.SinglePlayerAI;
            Intents = null;
        }
    }
}
