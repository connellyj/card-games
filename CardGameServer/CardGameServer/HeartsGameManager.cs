using CardGameServer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public class HeartsGameManager : GameManager
    {
        // ********** Static member variables ********** //

        // Total points that exist in a round
        private static readonly int ALL_POINTS = 26;

        // How many points the queen of spades is worth
        private static readonly int QUEENIE = 13;

        // How many cards should be passed
        private static readonly int NUM_TO_PASS = 3;

        // Passing directions
        private static readonly int[] PASS_DIRS = new int[4] { 1, -1, 2, 0 };


        // ********** Member variables ********** //

        // List of received pass messages
        private List<PassMessage> PassMessages;

        // Index of the current pass direction
        private int CurPass;

        /// <summary>
        /// Constructor
        /// </summary>
        public HeartsGameManager() : base()
        {
            PassMessages = new List<PassMessage>();
            CurPass = 0;
        }

        /// <summary>
        /// Start a round by starting passing.
        /// </summary>
        /// <param name="dealer"> The current dealer </param>
        protected override void DoStartRound(int dealer)
        {
            StartPass();
        }

        /// <summary>
        /// The first trick must start with the 2 of clubs.
        /// </summary>
        /// <param name="hand"> The player's hand </param>
        /// <returns> Valid cards that can be played </returns>
        protected override Card[] DoGetFirstTrickValidCards(List<Card> hand)
        {
            return hand.Where(c => c.Rank == "2" && c.Suit == "C").ToArray();
        }

        /// <summary>
        /// If this is the first trick, remove hearts and the queen of spades from the base class valid cards,
        /// otherwise just return the base class valid cards.
        /// </summary>
        /// <param name="hand"> The player's hand </param>
        /// <param name="trick"> The current trick </param>
        /// <param name="isFirstTrick"> Whether this trick is the first trick of the round </param>
        /// <returns></returns>
        protected override Card[] DoGetValidCards(List<Card> hand, List<Card> trick, bool isFirstTrick)
        {
            Card[] orig = base.DoGetValidCards(hand, trick, isFirstTrick);
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

        /// <summary>
        /// Update score based on the trick.
        /// </summary>
        /// <param name="trick"> The finished trick </param>
        /// <param name="winningPlayer"> The player who won the trick </param>
        protected override void DoScoreTrick(List<Card> trick, Player winningPlayer)
        {
            foreach (Card c in trick)
            {
                if (c.Suit == "H")
                {
                    winningPlayer.SecretScore++;
                }
                if (c.Suit == "S" && c.Rank == "Q")
                {
                    winningPlayer.SecretScore += QUEENIE;
                }
            }
        }

        /// <summary>
        /// Handle scoring when someone shoots the moon, otherwise update according to the base class.
        /// </summary>
        protected override void DoUpdateScores()
        {
            Player shootPlayer = GetPlayers().Where(p => p.SecretScore == ALL_POINTS).SingleOrDefault();
            if (shootPlayer != null)
            {
                foreach (Player p in GetPlayers())
                {
                    if (p.Uid != shootPlayer.Uid)
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

        /// <summary>
        /// The winning player is the one with the lowest score.
        /// </summary>
        /// <returns> The name of the winning player </returns>
        protected override string DoGetGameWinningPlayer()
        {
            return GetPlayers().OrderBy(p => p.Score).First().Name;
        }

        /// <summary>
        /// Handle a PassMessage.
        /// </summary>
        /// <param name="passMessage"></param>
        protected override void HandlePass(PassMessage passMessage)
        {
            // Add pass message
            PassMessages.Where(pm => pm.PassingPlayer == passMessage.PassingPlayer).Single().PassedCards = passMessage.PassedCards;
            
            // Continue once all passes have been completed
            if (PassMessages.Where(pm => pm.PassedCards != null).Count() == GetNumPlayers())
            {
                foreach (PassMessage pm in PassMessages)
                {
                    if (pm.PassingPlayer != pm.PassingTo)
                    {
                        // Update players' cards
                        Player passingToPlayer = GetPlayers().Where(p => p.Name == pm.PassingTo).Single();
                        Player passingPlayer = GetPlayers().Where(p => p.Name == pm.PassingPlayer).Single();
                        passingToPlayer.Cards.AddRange(pm.PassedCards);
                        passingPlayer.Cards.RemoveAll(c => pm.PassedCards.Any(h => h.Equals(c)));

                        // Send new cards to player
                        Server.Instance().Send(pm, passingToPlayer.Uid);
                    }
                }

                // Initiate first trick
                List<Player> players = GetPlayers();
                int startingPlayer = players.IndexOf(players.Where(p => p.Cards.Any(c => c.Suit == "C" && c.Rank == "2")).Single());
                StartTrick(startingPlayer, true);

                // Reset pass messages
                PassMessages = new List<PassMessage>();
            }
        }

        /// <summary>
        /// Start passing round.
        /// </summary>
        private void StartPass()
        {
            List<Player> players = GetPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                // Get player to pass to
                int idx = (i + PASS_DIRS[CurPass]) % players.Count;
                if (idx < 0)
                {
                    idx = players.Count() + idx;
                }
                string passingTo = players[idx].Name;

                // Update and send pass message
                PassMessage passMessage = new PassMessage(players[i].Name, NUM_TO_PASS, passingTo);
                Console.WriteLine("i: " + i.ToString());
                Console.WriteLine("idx: " + idx.ToString());
                Console.WriteLine(JsonConvert.SerializeObject(passMessage));
                PassMessages.Add(passMessage);
                Server.Instance().Send(passMessage, players[i].Uid);
            }

            // Update pass direction
            Console.WriteLine("CurPass: " + CurPass.ToString());
            CurPass = (CurPass + 1) % PASS_DIRS.Length;
            Console.WriteLine("CurPass: " + CurPass.ToString());
        }
    }
}
