namespace MythMayhem.Gameplay
{
    public static class MatchRules
    {
        public static MatchOutcome EvaluateOutcome(int playerHealth, int opponentHealth)
        {
            bool playerDead = playerHealth <= 0;
            bool opponentDead = opponentHealth <= 0;

            if (!playerDead && !opponentDead)
                return MatchOutcome.None;

            if (playerDead && opponentDead)
                return MatchOutcome.Draw;

            return playerDead ? MatchOutcome.OpponentWin : MatchOutcome.PlayerWin;
        }
    }
}
