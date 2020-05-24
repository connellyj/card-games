using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public class PinochleGameManager : GameManager
    {
        // ********** Static member variables ********** //

        // Static meld point values
        public static readonly int MIN_BID = 20;
        public static readonly int ACES_AROUND = 10;
        public static readonly int KINGS_AROUND = 8;
        public static readonly int QUEENS_AROUND = 6;
        public static readonly int JACKS_AROUND = 4;
        public static readonly int DOUBLE_AROUND_MULTIPLIER = 10;
        public static readonly int MARRIAGE = 2;
        public static readonly int TRUMP_MARRIAGE = 4;
        public static readonly int PINOCHLE = 4;
        public static readonly int DOUBLE_PINOCHLE = 30;
        public static readonly int TRUMP_NINE = 1;
        public static readonly int TRUMP_RUN = 15;

        // Static list of card ranks
        private static readonly List<string> Ranks = new List<string>() { "9", "J", "Q", "K", "10", "A" };

        // Number of suits in the deck
        public static readonly int NUM_SUITS = GetSuits().Count;

        // Number of ranks in the deck
        public static readonly int NUM_RANKS = Ranks.Count;

        // The number of players required to play
        private static readonly int MIN_PLAYERS = 3;

        // The number of cards in each players hand
        private static readonly int NUM_CARDS_IN_HAND = 15;


        // ********** Member variables ********** //

        // Cards in the current kitty
        private Card[] Kitty;

        // Players that have passed the current bid
        private List<int> PassedPlayers;

        // Current trump suit
        private string Trump;

        // Last person to bid
        private int LastBidder;

        // The current bid value
        private int CurBid;

        // How many players have submitted meld
        private int NumMeld;

        /// <summary>
        /// Returns the mininum number of players required to start the game.
        /// </summary>
        /// <returns> Minimum number of players </returns>
        public static new int MinPlayers()
        {
            return MIN_PLAYERS;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public PinochleGameManager() : base()
        {
            PassedPlayers = new List<int>();
            CurBid = MIN_BID;
            NumMeld = 0;
        }

        /// <summary>
        /// Shuffle a Pinochle deck
        /// </summary>
        /// <returns> Full shuffled list of Card </returns>
        protected override Card[] DoShuffle()
        {
            List<string> doubleRank = new List<string>();
            doubleRank.AddRange(Ranks);
            doubleRank.AddRange(Ranks);
            return Deck.Shuffle(GetSuits(), doubleRank);
        }

        /// <summary>
        /// Put extra cards in the kitty
        /// </summary>
        /// <param name="cards"> Extra cards after dealing </param>
        protected override void DoDealExtraCards(IEnumerable<Card> cards)
        {
            Kitty = cards.ToArray();
        }

        /// <summary>
        /// Starts a round by starting the bid and sending out meld points
        /// </summary>
        /// <param name="dealer"> The index of the dealer </param>
        protected override void DoStartRound(int dealer)
        {
            SendPlayerHands();
            StartBid(GetCurrentPlayer(), dealer);
            SendMeldPoints();
        }

        /// <summary>
        /// Get playable cards
        /// </summary>
        /// <param name="hand"> The player's hand </param>
        /// <param name="trick"> The current trick </param>
        /// <param name="isFirstTrick"> WHether this is the first trick of the round </param>
        /// <returns></returns>
        protected override Card[] DoGetValidCards(List<Card> hand, List<Card> trick, bool isFirstTrick)
        {
            return PinochleTrickDecider.ValidCards(hand, trick, Trump).ToArray();
        }

        /// <summary>
        /// Decide a Pinochle trick
        /// </summary>
        /// <param name="trick"> Cards in the trick </param>
        /// <returns> Index of the winning card </returns>
        protected override int DoDecideTrick(List<Card> trick)
        {
            return PinochleTrickDecider.WinningCard(trick, Trump);
        }

        /// <summary>
        /// When a trick is done, add points to players' scores
        /// </summary>
        /// <param name="trick"> The trick </param>
        /// <param name="winningPlayer"> The player who won the trick </param>
        protected override void DoScoreTrick(List<Card> trick, Player winningPlayer)
        {
            foreach (Card c in trick)
            {
                if (PinochleTrickDecider.IsPoint(c))
                {
                    winningPlayer.SecretScore++;
                }
            }
        }

        /// <summary>
        /// Add point for last trick
        /// </summary>
        /// <param name="winningPlayer"> Player who won the last trick </param>
        protected override void DoLastTrick(int winningPlayer)
        {
            GetPlayer(winningPlayer).SecretScore++;
        }

        /// <summary>
        /// Update scores
        /// </summary>
        protected override void DoUpdateScores()
        {
            // Handle player went set
            Player biddingPlayer = GetPlayer(LastBidder);
            if (biddingPlayer.SecretScore + biddingPlayer.MeldScore < CurBid)
            {
                biddingPlayer.MissedBidBy = CurBid - (biddingPlayer.SecretScore + biddingPlayer.MeldScore);
                biddingPlayer.Score -= CurBid;
                biddingPlayer.SecretScore = 0;
                biddingPlayer.MeldScore = 0;
            }

            // Update scores
            foreach (Player p in GetPlayers())
            {
                if (!p.TookATrick)
                {
                    p.MeldScore = 0;
                }
                p.Score += p.SecretScore;
                p.Score += p.MeldScore;
            }
        }

        /// <summary>
        /// Get minimum number of players required for a game
        /// </summary>
        /// <returns> Minimum number of players </returns>
        protected override int GetMinPlayers()
        {
            return MinPlayers();
        }

        /// <summary>
        /// Get the number of cards in a players hand
        /// </summary>
        /// <returns> Number of cards in a hand </returns>
        protected override int GetNumCardsInHand()
        {
            return NUM_CARDS_IN_HAND;
        }

        /// <summary>
        /// Handle a BidMessage.
        /// </summary>
        /// <param name="bidMessage"></param>
        protected override void HandleBid(BidMessage bidMessage)
        {
            // Broadcast bid to all players
            Broadcast(bidMessage);

            // Parse bid
            CurBid = bidMessage.Bid;
            if (bidMessage.Bid != 0)
            {
                LastBidder = GetCurrentPlayerIndex();
            }
            else
            {
                CurBid = bidMessage.CurBid;
                PassedPlayers.Add(GetCurrentPlayerIndex());
            }

            // Get next non-passed player
            NextPlayer();
            while (PassedPlayers.Contains(GetCurrentPlayerIndex()) && GetCurrentPlayerIndex() != LastBidder)
            {
                NextPlayer();
            }

            // Handle last bid
            if (GetCurrentPlayerIndex() == LastBidder)
            {
                Broadcast(new BidMessage(GetCurrentPlayer().Name, CurBid, CurBid));
                PassedPlayers = new List<int>();
                StartKitty(GetCurrentPlayerIndex());
            }

            // Initiate next bid
            else
            {
                Player player = GetCurrentPlayer();
                Broadcast(new BidMessage(player.Name, CurBid));
            }
        }

        /// <summary>
        /// Handle a KittyMessage
        /// </summary>
        /// <param name="kittyMessage"></param>
        protected override void HandleKitty(KittyMessage kittyMessage)
        {
            // Update player model
            Player player = GetCurrentPlayer();
            player.Cards.AddRange(Kitty);
            foreach (Card c in kittyMessage.Kitty)
            {
                player.Cards.Remove(c);
                if (PinochleTrickDecider.IsPoint(c))
                {
                    player.SecretScore++;
                }
            }

            // Initiate trump round
            StartTrump(GetCurrentPlayerIndex());
        }

        /// <summary>
        /// Handle a TrumpMessage.
        /// </summary>
        /// <param name="trumpMessage"></param>
        protected override void HandleTrump(TrumpMessage trumpMessage)
        {
            // Broadcast to all players
            Broadcast(trumpMessage);

            // Set trump suit
            Trump = trumpMessage.TrumpSuit;

            // Initiate meld round
            StartMeld(trumpMessage.TrumpSuit);
        }

        /// <summary>
        /// Handle a MeldMessage
        /// </summary>
        /// <param name="meldMessage"></param>
        protected override void HandleMeld(MeldMessage meldMessage)
        {
            // Broadcast to all players
            Broadcast(meldMessage);

            // Start first trick if all meld is in
            NumMeld++;
            if (NumMeld == GetNumPlayers())
            {
                NumMeld = 0;
                StartTrick(LastBidder);
            }
        }

        /// <summary>
        /// Start bidding round.
        /// </summary>
        /// <param name="player"> Player who should bid first </param>
        /// <param name="dealer"> Current dealer </param>
        private void StartBid(Player player, int dealer)
        {
            LastBidder = dealer;
            Broadcast(new BidMessage(player.Name, MIN_BID));
        }

        /// <summary>
        /// Start kitty round.
        /// </summary>
        /// <param name="player"> Index of the player who got the kitty </param>
        private void StartKitty(int player)
        {
            Broadcast(new KittyMessage(Kitty, GetPlayer(player).Name));
        }

        /// <summary>
        /// Start trump round.
        /// </summary>
        /// <param name="player"> Index of the player who should bid first </param>
        private void StartTrump(int player)
        {
            Broadcast(new TrumpMessage(GetPlayer(player).Name));
        }

        /// <summary>
        /// Start meld round.
        /// </summary>
        /// <param name="trump"> Trump suit </param>
        private void StartMeld(string trump)
        {
            foreach (Player p in GetPlayers())
            {
                MeldCounter counter = new MeldCounter(p.Cards, trump);
                p.MeldScore = counter.TotalMeld();
                MeldMessage message = new MeldMessage(p.Name, trump, p.MeldScore)
                {
                    AcesAround = counter.AcesAround(),
                    KingsAround = counter.KingsAround(),
                    QueensAround = counter.QueensAround(),
                    JacksAround = counter.JacksAround(),
                    TrumpNine = counter.Nines(),
                    Run = counter.Runs(),
                    ClubsMarriage = counter.ClubsMarriage(),
                    DiamondsMarriage = counter.DiamondsMarriage(),
                    SpadesMarriage = counter.SpadesMarriage(),
                    HeartsMarriage = counter.HeartsMarriage(),
                    Pinochle = counter.Pinochle()
                };
                Server.Instance().Send(message, p.Uid);
            }
        }

        /// <summary>
        /// Send out meld points.
        /// </summary>
        private void SendMeldPoints()
        {
            MeldPointsMessage meldPoints = new MeldPointsMessage(ACES_AROUND, KINGS_AROUND, QUEENS_AROUND, JACKS_AROUND,
                DOUBLE_AROUND_MULTIPLIER, MARRIAGE, TRUMP_MARRIAGE, PINOCHLE, DOUBLE_PINOCHLE, TRUMP_NINE, TRUMP_RUN);
            Broadcast(meldPoints);
        }
    }
}
