using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public class HeartsGameManager : GameManager
    {
        private static int ALL_POINTS = 26;

        public HeartsGameManager() : base()
        {
        }

        public static int MinPlayers()
        {
            return 4;
        }

        protected override void DoTrick(List<Card> trick, Player winningPlayer)
        {
            foreach (Card c in trick)
            {
                if (c.Suit == "H")
                {
                    winningPlayer.SecretScore++;
                }
                if (c.Suit == "S" && c.Rank == "Q")
                {
                    winningPlayer.SecretScore++;
                }
            }
        }

        protected override void DoUpdateScores()
        {
            Player shootPlayer = GetPlayers().Where(p => p.SecretScore == ALL_POINTS).SingleOrDefault();
            if (shootPlayer != null)
            {
                foreach (Player p in GetPlayers())
                {
                    if (p != shootPlayer)
                    {
                        p.Score += ALL_POINTS;
                    }
                    p.SecretScore = 0;
                }
            }
            else
            {
                base.DoUpdateScores();
            }
        }

        protected override string GetGameWinningPlayer()
        {
            return GetPlayers().OrderBy(p => p.Score).First().Name;
        }

        protected override int GetMinPlayers()
        {
            return MinPlayers();
        }
    }
}
