using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public class HeartsGameManager : GameManager
    {
        private List<PassMessage> PassMessages;
        private int CurPass;

        private static readonly int ALL_POINTS = 26;
        private static readonly int NUM_TO_PASS = 3;
        private static readonly int[] PASS_DIRS = new int[4] { 1, -1, 2, 0 };

        public HeartsGameManager() : base()
        {
            PassMessages = new List<PassMessage>();
            CurPass = 0;
        }

        public static int MinPlayers()
        {
            return 4;
        }

        protected override int DoSetStartingPlayer()
        {
            List<Player> players = GetPlayers();
            return players.IndexOf(players.Where(p => p.Cards.Any(c => c.Suit == "C" && c.Rank == "2")).Single());
        }

        protected override void DoStartRound(int dealer)
        {
            StartPass();
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
                }
            }
            else
            {
                base.DoUpdateScores();
            }
        }

        protected override Card[] GetFirstRoundValidCards(List<Card> hand)
        {
            return hand.Where(c => c.Rank == "2" && c.Suit == "C").ToArray();
        }

        protected override Card[] GetValidCards(List<Card> hand, List<Card> trick, bool isFirstTrick)
        {
            Card[] orig = base.GetValidCards(hand, trick, isFirstTrick);
            if (isFirstTrick)
            {
                Card[] noHearts = orig.Where(c => c.Suit != "H" && !(c.Suit == "S" && c.Rank == "Q")).ToArray();
                if (noHearts.Length > 0)
                {
                    return noHearts;
                }
            }
            return orig;
        }

        protected override string GetGameWinningPlayer()
        {
            return GetPlayers().OrderBy(p => p.Score).First().Name;
        }

        protected override int GetMinPlayers()
        {
            return MinPlayers();
        }

        protected override void HandlePass(PassMessage passMessage)
        {
            PassMessages.Where(pm => pm.PassingPlayer == passMessage.PassingPlayer).Single().PassedCards = passMessage.PassedCards;
            if (PassMessages.Where(pm => pm.PassedCards != null).Count() == GetNumPlayers())
            {
                foreach (PassMessage pm in PassMessages)
                {
                    if (pm.PassingPlayer != pm.PassingTo)
                    {
                        Player passingToPlayer = GetPlayers().Where(p => p.Name == pm.PassingTo).Single();
                        Player passingPlayer = GetPlayers().Where(p => p.Name == pm.PassingPlayer).Single();
                        passingToPlayer.Cards.AddRange(pm.PassedCards);
                        passingPlayer.Cards.RemoveAll(c => pm.PassedCards.Any(h => h.Equals(c)));
                        Server.Instance().Send(pm, passingToPlayer.Uid);
                    }
                }
                StartTurn(GetCurrentPlayerIndex(), true);
            }
        }

        private void StartPass()
        {
            List<Player> players = GetPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                int idx = (i + PASS_DIRS[CurPass]) % players.Count;
                if (idx < 0)
                {
                    idx = players.Count() + idx;
                }
                string passingTo = players[idx].Name;
                PassMessage passMessage = new PassMessage(players[i].Name, NUM_TO_PASS, passingTo);
                PassMessages.Add(passMessage);
                Server.Instance().Send(passMessage, players[i].Uid);
            }
            CurPass++;
        }
    }
}
