using NUnit.Framework;
using MythMayhem.Gameplay;

namespace MythMayhem.Tests
{
    public class MatchRulesTests
    {
        [Test]
        public void EvaluateOutcome_BothAlive_ReturnsNone()
        {
            Assert.AreEqual(MatchOutcome.None, MatchRules.EvaluateOutcome(30, 30));
        }

        [Test]
        public void EvaluateOutcome_OnlyOpponentDead_PlayerWins()
        {
            Assert.AreEqual(MatchOutcome.PlayerWin, MatchRules.EvaluateOutcome(12, 0));
        }

        [Test]
        public void EvaluateOutcome_OnlyPlayerDead_OpponentWins()
        {
            Assert.AreEqual(MatchOutcome.OpponentWin, MatchRules.EvaluateOutcome(0, 12));
        }

        [Test]
        public void EvaluateOutcome_BothDead_IsDraw()
        {
            Assert.AreEqual(MatchOutcome.Draw, MatchRules.EvaluateOutcome(0, 0));
        }
    }
}
