using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public class PinochleGameManager : GameManager
    {
        public static readonly int NUM_KITTY_CARDS = 3;
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

        private Card[] Kitty;
        private List<int> PassedPlayers;

        private string Trump;
        private int LastBidder;
        private int CurBid;
        private int NumMeld;

        public PinochleGameManager() : base()
        {
            PassedPlayers = new List<int>();
            CurBid = MIN_BID;
            NumMeld = 0;
        }

        public static int MinPlayers()
        {
            return 3;
        }

        protected override void DoStartRound(int curPlayer, int dealer)
        {
            StartBid(GetPlayer(curPlayer), dealer);
            SendMeldPoints();
        }

        protected override int DoDecideTrick(List<Card> trick)
        {
            return TrickDecider.WinningCard(trick, Trump);
        }

        protected override void DoTrick(List<Card> trick, Player winningPlayer)
        {
            foreach (Card c in trick)
            {
                if (TrickDecider.IsPoint(c))
                {
                    winningPlayer.SecretScore++;
                }
            }
        }

        protected override void DoLastTrick(int winningPlayer)
        {
            // point for last trick
            GetPlayer(winningPlayer).SecretScore++;
        }

        protected override void DoUpdateScores()
        {
            Player biddingPlayer = GetPlayer(LastBidder);
            if (biddingPlayer.SecretScore + biddingPlayer.MeldScore < CurBid)
            {
                biddingPlayer.Score -= CurBid;
                biddingPlayer.SecretScore = 0;
                biddingPlayer.MeldScore = 0;
            }
            foreach (Player p in GetPlayers())
            {
                if (!p.TookATrick)
                {
                    p.MeldScore = 0;
                }
                p.Score += p.SecretScore;
                p.Score += p.MeldScore;
                p.SecretScore = 0;
                p.MeldScore = 0;
            }
        }

        protected override Card[] GetValidCards(List<Card> hand, List<Card> trick)
        {
            return TrickDecider.ValidCards(hand, trick, Trump).ToArray();
        }

        protected override int GetWinningPointTotal()
        {
            return 100;
        }

        protected override int GetMinPlayers()
        {
            return MinPlayers();
        }

        protected override int GetNumCardsInHand()
        {
            return 15;
        }

        protected override void DealExtraCards(IEnumerable<Card> cards)
        {
            Kitty = cards.ToArray();
        }

        protected override void HandleBid(BidMessage message)
        {
            // broadcast bid to all players
            Broadcast(message);

            // parse bid
            CurBid = message.Bid;
            if (message.Bid != 0)
            {
                LastBidder = GetCurrentPlayerIndex();
            }
            else
            {
                CurBid = message.CurBid;
                PassedPlayers.Add(GetCurrentPlayerIndex());
            }

            // get next non-passed player
            NextPlayer();
            while (PassedPlayers.Contains(GetCurrentPlayerIndex()) && GetCurrentPlayerIndex() != LastBidder)
            {
                NextPlayer();
            }

            // initiate next turn
            if (GetCurrentPlayerIndex() == LastBidder)
            {
                Broadcast(new BidMessage(GetCurrentPlayer().Name, CurBid, CurBid));
                PassedPlayers = new List<int>();
                StartKitty(GetCurrentPlayerIndex());
            }
            else
            {
                Player player = GetCurrentPlayer();
                Server.Instance().Send(new BidMessage(player.Name, CurBid), player.Uid);
            }
        }

        protected override void HandleKitty(KittyMessage message)
        {
            // update player model
            Player player = GetCurrentPlayer();
            player.Cards.AddRange(Kitty);
            foreach (Card c in message.Kitty)
            {
                player.Cards.Remove(c);
                if (TrickDecider.IsPoint(c))
                {
                    player.SecretScore++;
                }
            }

            // initiate next turn
            StartTrump(GetCurrentPlayerIndex());
        }

        protected override void HandleTrump(TrumpMessage message)
        {
            // broadcast to all players
            Broadcast(message);

            Trump = message.TrumpSuit;

            // initiate next turn
            StartMeld(message.TrumpSuit);
        }

        protected override void HandleMeld(MeldMessage message)
        {
            // broadcast to all players
            Broadcast(message);
            BroadcastScore(message.PlayerName, true);

            NumMeld++;

            // initiate next turn
            if (NumMeld == GetNumPlayers())
            {
                NumMeld = 0;
                StartTurn(LastBidder);
            }
        }

        private void StartBid(Player player, int dealer)
        {
            LastBidder = dealer;
            Server.Instance().Send(new BidMessage(player.Name, MIN_BID), player.Uid);
        }

        private void StartKitty(int player)
        {
            Broadcast(new KittyMessage(Kitty, GetPlayer(player).Name));
        }

        private void StartTrump(int player)
        {
            Server.Instance().Send(new TrumpMessage(GetPlayer(player).Name), GetPlayer(player).Uid);
        }

        private void StartMeld(string trump)
        {
            foreach (Player p in GetPlayers())
            {
                MeldCounter counter = new MeldCounter(p.Cards, trump);
                p.MeldScore = counter.TotalMeld();
                MeldMessage message = new MeldMessage(p.Name, trump)
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

        private void SendMeldPoints()
        {
            MeldPointsMessage meldPoints = new MeldPointsMessage(ACES_AROUND, KINGS_AROUND, QUEENS_AROUND, JACKS_AROUND,
                DOUBLE_AROUND_MULTIPLIER, MARRIAGE, TRUMP_MARRIAGE, PINOCHLE, DOUBLE_PINOCHLE, TRUMP_NINE, TRUMP_RUN);
            Broadcast(meldPoints);
        }
    }
}
