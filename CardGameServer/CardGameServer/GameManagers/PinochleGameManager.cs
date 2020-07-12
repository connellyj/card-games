using CardGameServer.GameMechanics;
using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer.GameManagers
{
    public class PinochleGameManager : GameManager
    {
        // ********** Static member variables ********** //

        // Static list of card ranks
        private static readonly List<string> Ranks = new List<string>() { "9", "J", "Q", "K", "10", "A" };

        // Minimum bid
        public static readonly int MIN_BID = 20;

        // The number of players required to play
        private static readonly int MIN_PLAYERS = 3;

        // The number of cards in each players hand
        private static readonly int NUM_CARDS_IN_HAND = 15;


        // ********** Member variables ********** //

        // Cards in the current kitty
        private Card[] Kitty;

        // Players that have passed the current bid
        private List<int> PassedPlayers;

        // Last person to bid
        private int LastBidder;

        // The current bid value
        private int CurBid;

        // How many players have submitted meld
        private int NumMeld;

        /// <summary>
        /// Constructor
        /// </summary>
        public PinochleGameManager() : base(new PinochleTrickDecider())
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
            return Deck.Shuffle(Suits, doubleRank);
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
        /// <returns> List of messages to be sent </returns>
        protected override MessagePackets DoStartRound()
        {
            MessagePackets messages = new MessagePackets();
            messages.Add(GetStartMessages());
            messages.Add(StartBid(GetCurrentPlayer(), GetDealerIndex()));
            messages.Add(GetMeldPointsMessages());
            return messages;
        }

        /// <summary>
        /// When a trick is done, add points to players' scores
        /// </summary>
        /// <param name="trick"> The trick </param>
        /// <param name="winningPlayer"> The player who won the trick </param>
        /// <returns> List of messages to be sent </returns>
        protected override MessagePackets DoScoreTrick(List<Card> trick, Player winningPlayer)
        {
            foreach (Card c in trick)
            {
                if (PinochleTrickDecider.IsPoint(c))
                {
                    winningPlayer.SecretScore++;
                }
            }

            return new MessagePackets();
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
            foreach (Player p in GetPlayersMutable())
            {
                if (p.TricksTaken == 0)
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
        protected override int DoGetMinPlayers()
        {
            return MIN_PLAYERS;
        }

        /// <summary>
        /// Get the number of cards in a players hand
        /// </summary>
        /// <returns> Number of cards in a hand </returns>
        protected override int DoGetNumCardsInHand()
        {
            return NUM_CARDS_IN_HAND;
        }

        /// <summary>
        /// Handle a BidMessage.
        /// </summary>
        /// <param name="bidMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public override MessagePackets HandleBid(BidMessage bidMessage)
        {
            MessagePackets messages = new MessagePackets();

            // Broadcast bid to all players
            messages.Add(GetBroadcastMessage(bidMessage));

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
                messages.Add(GetBroadcastMessage(new BidMessage(GetCurrentPlayer().Name, CurBid, CurBid)));
                PassedPlayers = new List<int>();
                messages.Add(StartKitty(GetCurrentPlayerIndex()));
            }

            // Initiate next bid
            else
            {
                Player player = GetCurrentPlayer();
                messages.Add(GetBroadcastMessage(new BidMessage(player.Name, CurBid)));
            }

            return messages;
        }

        /// <summary>
        /// Handle a KittyMessage
        /// </summary>
        /// <param name="kittyMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public override MessagePackets HandleKitty(KittyMessage kittyMessage)
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
            return StartTrump(GetCurrentPlayerIndex());
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

            // Set trump suit
            ((PinochleTrickDecider)GetDecider()).Trump = trumpMessage.TrumpSuit;

            // Initiate meld round
            messages.Add(StartMeld(trumpMessage.TrumpSuit));

            return messages;
        }

        /// <summary>
        /// Handle a MeldMessage
        /// </summary>
        /// <param name="meldMessage"></param>
        /// <returns> List of messages to be sent </returns>
        public override MessagePackets HandleMeld(MeldMessage meldMessage)
        {
            MessagePackets messages = new MessagePackets();

            // Broadcast to all players
            messages.Add(GetBroadcastMessage(meldMessage));

            // Start first trick if all meld is in
            NumMeld++;
            if (NumMeld == GetNumPlayers())
            {
                NumMeld = 0;
                messages.Add(StartTrick(LastBidder));
            }

            return messages;
        }

        /// <summary>
        /// Start bidding round.
        /// </summary>
        /// <param name="player"> Player who should bid first </param>
        /// <param name="dealer"> Current dealer </param>
        /// <returns> List of messages to be sent </returns>
        private MessagePackets StartBid(Player player, int dealer)
        {
            LastBidder = dealer;
            return GetBroadcastMessage(new BidMessage(player.Name, MIN_BID));
        }

        /// <summary>
        /// Start kitty round.
        /// </summary>
        /// <param name="player"> Index of the player who got the kitty </param>
        /// <returns> List of messages to be sent </returns>
        private MessagePackets StartKitty(int player)
        {
            return GetBroadcastMessage(new KittyMessage(Kitty, GetPlayer(player).Name));
        }

        /// <summary>
        /// Start trump round.
        /// </summary>
        /// <param name="player"> Index of the player who should bid first </param>
        /// <returns> List of messages to be sent </returns>
        private MessagePackets StartTrump(int player)
        {
            return GetBroadcastMessage(new TrumpMessage(GetPlayer(player).Name));
        }

        /// <summary>
        /// Start meld round.
        /// </summary>
        /// <param name="trump"> Trump suit </param>
        /// <returns> List of messages to be sent </returns>
        private MessagePackets StartMeld(string trump)
        {
            MessagePackets messages = new MessagePackets();

            foreach (Player p in GetPlayersMutable())
            {
                MeldCounter counter = new MeldCounter(p.Cards, trump, Suits.Count, Ranks.Count);
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
                messages.Add(message, p.Uid);
            }

            return messages;
        }

        /// <summary>
        /// Send out meld points.
        /// </summary>
        /// <returns> List of messages to be sent </returns>
        private MessagePackets GetMeldPointsMessages()
        {
            MeldPointsMessage meldPoints = new MeldPointsMessage(
                MeldCounter.ACES_AROUND, 
                MeldCounter.KINGS_AROUND,
                MeldCounter.QUEENS_AROUND,
                MeldCounter.JACKS_AROUND,
                MeldCounter.DOUBLE_AROUND_MULTIPLIER,
                MeldCounter.MARRIAGE,
                MeldCounter.TRUMP_MARRIAGE,
                MeldCounter.PINOCHLE,
                MeldCounter.DOUBLE_PINOCHLE,
                MeldCounter.TRUMP_NINE,
                MeldCounter.TRUMP_RUN);
            return GetBroadcastMessage(meldPoints);
        }
    }
}
