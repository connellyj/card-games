using CardGameServer.GameMechanics;
using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer.GameManagers
{
    public class MizerkaGameManager : GameManager
    {
        // ********** Static member variables ********** //

        // The number of players required to play
        private static readonly int MIN_PLAYERS = 3;

        // Extra options for "trump"
        private static readonly string[] ExtraTrumpOptions = new string[2] { "No Trump", "Mizerka" };

        // All options for trump
        private static readonly List<string> TrumpOptions = new List<string>(Suits.Union(ExtraTrumpOptions));

        // Number of tricks required to break even
        private static readonly int[] TricksNeeded = new int[3] { 7, 5, 1 };


        // ********** Member variables ********** //

        // The 4th hand, the talon
        private List<Card> Talon;

        public MizerkaGameManager() : base(new TrumpDecider())
        {
            Talon = new List<Card>();
        }

        /// <summary>
        /// Deal the extra hand into the talon
        /// </summary>
        /// <param name="cards"> The remaining cards that weren't dealt to players </param>
        protected override void DoDealExtraCards(IEnumerable<Card> cards)
        {
            Talon.AddRange(cards);
        }

        /// <summary>
        /// Start the round with dealing first 5 cards and deciding trump
        /// </summary>
        /// <returns> List of messages to be sent </returns>
        protected override MessagePackets DoStartRound()
        {
            MessagePackets messages = new MessagePackets();
            Player p = GetPlayer(GetDealerIndex());
            messages.Add(new StartMessage(p.Cards.Take(5).ToArray()), p.Uid);
            messages.Add(GetBroadcastMessage(new TrumpMessage(p.Name, unavailableOptions: p.TrumpUsed.ToArray(), extraOptions: ExtraTrumpOptions)));
            return messages;
        }

        /// <summary>
        /// TricksTaken used for scoring, so just send out trick info
        /// </summary>
        /// <param name="trick"> The completed trick </param>
        /// <param name="winningPlayer"> The index of the player that won the trick </param>
        /// <returns> List of messages to be sent </returns>
        protected override MessagePackets DoScoreTrick(List<Card> trick, Player winningPlayer)
        {
            return GetTrickInfoMessage();
        }

        /// <summary>
        /// Add difference between required tricks and tricks taken.
        /// </summary>
        protected override void DoUpdateScores()
        {
            List<Player> players = GetPlayersMutable();
            int dealer = GetDealerIndex();
            for (int i = 0; i < players.Count; i++)
            {
                int tricksLeft = GetTricksNeeded(i, dealer);
                players[i].Score -= tricksLeft;
            }
        }

        /// <summary>
        /// Returns true when all players have chosen every option for trump.
        /// </summary>
        /// <returns> Whether the game is over </returns>
        protected override bool DoShouldGameEnd()
        {
            return GetPlayersMutable().All(p => p.TrumpUsed.Count == TrumpOptions.Count);
        }

        /// <summary>
        /// Returns min number of players required.
        /// </summary>
        /// <returns> Number of players required to start the game </returns>
        protected override int DoGetMinPlayers()
        {
            return MIN_PLAYERS;
        }

        /// <summary>
        /// Handle a TrumpMessage.
        /// </summary>
        /// <param name="trumpMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public override MessagePackets HandleTrump(TrumpMessage trumpMessage)
        {
            MessagePackets messages = new MessagePackets();

            // Broadcast to all players
            messages.Add(GetBroadcastMessage(trumpMessage));

            // Save trump and update player model
            ((TrumpDecider)GetDecider()).Trump = trumpMessage.TrumpSuit;
            Player choosingPlayer = GetPlayersMutable().Where(p => p.Name == trumpMessage.ChoosingPlayer).Single();
            choosingPlayer.TrumpUsed.Add(trumpMessage.TrumpSuit);

            // Send out cards and start first trick
            messages.Add(GetStartMessages());
            messages.Add(GetTrickInfoMessage());
            messages.Add(StartTrick(GetDealerIndex()));
            return messages;
        }

        /// <summary>
        /// Helper method to send TrickInfoMessage
        /// </summary>
        /// <returns> List of messages to be sent </returns>
        private MessagePackets GetTrickInfoMessage()
        {
            List<Player> players = GetPlayersMutable();
            int dealer = GetDealerIndex();
            Dictionary<string, int> trickInfo = new Dictionary<string, int>();
            for (int i = 0; i < players.Count; i++)
            {
                int tricksLeft = GetTricksNeeded(i, dealer);
                trickInfo.Add(players[i].Name, tricksLeft);
            }
            return GetBroadcastMessage(new TrickInfoMessage(trickInfo));
        }

        /// <summary>
        /// Helper method to get number of tricks still needed to break even
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="dealerIndex"></param>
        /// <returns></returns>
        private int GetTricksNeeded(int playerIndex, int dealerIndex)
        {
            int idx = playerIndex - dealerIndex;
            if (idx < 0)
            {
                idx = TricksNeeded.Length + idx;
            }
            return TricksNeeded[idx] - GetPlayer(playerIndex).TricksTaken;
        }
    }
}
