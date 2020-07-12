using CardGameServer.GameMechanics;
using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer.GameManagers
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
        public HeartsGameManager() : base(new HeartsTrickDecider())
        {
            PassMessages = new List<PassMessage>();
            CurPass = 0;
        }

        /// <summary>
        /// Start a round by starting passing.
        /// </summary>
        /// <returns> List of messages to be sent </returns>
        protected override MessagePackets DoStartRound()
        {
            MessagePackets messages = new MessagePackets();
            messages.Add(GetStartMessages());
            messages.Add(StartPass());
            return messages;
        }

        /// <summary>
        /// Update score based on the trick.
        /// </summary>
        /// <param name="trick"> The finished trick </param>
        /// <param name="winningPlayer"> The player who won the trick </param>
        /// <returns> List of messages to be sent </returns>
        protected override MessagePackets DoScoreTrick(List<Card> trick, Player winningPlayer)
        {
            foreach (Card c in trick)
            {
                if (c.Suit == "H")
                {
                    winningPlayer.SecretScore++;
                    ((HeartsTrickDecider)GetDecider()).ArePointsBroken = true;
                }
                if (c.Suit == "S" && c.Rank == "Q")
                {
                    winningPlayer.SecretScore += QUEENIE;
                    ((HeartsTrickDecider)GetDecider()).ArePointsBroken = true;
                }
            }

            return new MessagePackets();
        }

        /// <summary>
        /// Handle scoring when someone shoots the moon, otherwise update according to the base class.
        /// </summary>
        protected override void DoUpdateScores()
        {
            Player shootPlayer = GetPlayersMutable().Where(p => p.SecretScore == ALL_POINTS).SingleOrDefault();
            if (shootPlayer != null)
            {
                foreach (Player p in GetPlayersMutable())
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
            return GetPlayersMutable().OrderBy(p => p.Score).First().Name;
        }

        /// <summary>
        /// Handle a PassMessage.
        /// </summary>
        /// <param name="passMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public override MessagePackets HandlePass(PassMessage passMessage)
        {
            MessagePackets messages = new MessagePackets();

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
                        Player passingToPlayer = GetPlayersMutable().Where(p => p.Name == pm.PassingTo).Single();
                        Player passingPlayer = GetPlayersMutable().Where(p => p.Name == pm.PassingPlayer).Single();
                        passingToPlayer.Cards.AddRange(pm.PassedCards);
                        passingPlayer.Cards.RemoveAll(c => pm.PassedCards.Any(h => h.Equals(c)));

                        // Send new cards to player
                        messages.Add(pm, passingToPlayer.Uid);
                    }
                }

                // Initiate first trick
                List<Player> players = GetPlayersMutable();
                int startingPlayer = players.IndexOf(players.Where(p => p.Cards.Any(c => c.Suit == "C" && c.Rank == "2")).Single());
                messages.Add(StartTrick(startingPlayer, true));

                // Reset pass messages
                PassMessages = new List<PassMessage>();
            }

            return messages;
        }

        /// <summary>
        /// Start passing round.
        /// </summary>
        /// <returns> List of messages to be sent </returns>
        private MessagePackets StartPass()
        {
            MessagePackets messages = new MessagePackets();

            List<Player> players = GetPlayersMutable();
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
                PassMessages.Add(passMessage);
                messages.Add(passMessage, players[i].Uid);
            }

            // Update pass direction
            CurPass = (CurPass + 1) % PASS_DIRS.Length;

            return messages;
        }
    }
}
