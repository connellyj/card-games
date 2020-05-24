using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public class MizerkaGameManager : GameManager
    {
        // ********** Static member variables ********** //

        // The number of players required to play
        private static readonly int MIN_PLAYERS = 3;

        // Extra options for "trump"
        private static string[] ExtraTrumpOptions = new string[2] { "No Trump", "Mizerka" };

        // All options for trump
        private static List<string> TrumpOptions = new List<string>(GetSuits().Union(ExtraTrumpOptions));


        // ********** Member variables ********** //

        // The 4th hand, the talon
        private List<Card> Talon;

        // Trump suit for the current round
        private string Trump;

        /// <summary>
        /// Returns the mininum number of players required to start the game.
        /// </summary>
        /// <returns> Minimum number of players </returns>
        public static new int MinPlayers()
        {
            return MIN_PLAYERS;
        }

        public MizerkaGameManager()
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
        /// <param name="dealer"> The current dealer </param>
        protected override void DoStartRound(int dealer)
        {
            Player p = GetPlayer(dealer);
            Server.Instance().Send(new StartMessage(p.Cards.Take(5).ToArray()), p.Uid);
            Broadcast(new TrumpMessage(p.Name, availableOptions: TrumpOptions.Except(p.TrumpUsed).ToArray(), extraOptions: ExtraTrumpOptions));
        }

        /// <summary>
        /// Add a point for the trick taken
        /// </summary>
        /// <param name="trick"> The completed trick </param>
        /// <param name="winningPlayer"> The index of the player that won the trick </param>
        protected override void DoScoreTrick(List<Card> trick, Player winningPlayer)
        {
            winningPlayer.SecretScore++;
        }

        /// <summary>
        /// Add difference between required tricks and tricks taken.
        /// </summary>
        protected override void DoUpdateScores()
        {
            // TODO UPDATE SCORES
            // TODO SEND HOW MANY TRICKS PLAYERS NEED TO TAKE
        }

        /// <summary>
        /// Returns true when all players have chosen every option for trump.
        /// </summary>
        /// <returns> Whether the game is over </returns>
        protected override bool ShouldGameEnd()
        {
            return GetPlayers().All(p => p.TrumpUsed.Count == TrumpOptions.Count);
        }

        /// <summary>
        /// Handle a TrumpMessage.
        /// </summary>
        /// <param name="trumpMessage"></param>
        protected override void HandleTrump(TrumpMessage trumpMessage)
        {
            Broadcast(trumpMessage);
            Trump = trumpMessage.TrumpSuit;
            SendPlayerHands();
            List<Player> players = GetPlayers();
            StartTrick(players.IndexOf(players.Where(p => p.Name == trumpMessage.ChoosingPlayer).Single()));
        }
    }
}
