using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public class MeldCounter
    {
        private readonly IEnumerable<Card> Cards;
        private readonly string Trump;

        public MeldCounter(IEnumerable<Card> cards, string trump)
        {
            Cards = cards;
            Trump = trump;
        }

        public int TotalMeld()
        {
            int clubs = Trump == "C" ? ClubsMarriage() * PinochleGameManager.TRUMP_MARRIAGE : ClubsMarriage() * PinochleGameManager.MARRIAGE;
            int diamonds = Trump == "D" ? DiamondsMarriage() * PinochleGameManager.TRUMP_MARRIAGE : DiamondsMarriage() * PinochleGameManager.MARRIAGE;
            int spades = Trump == "S" ? SpadesMarriage() * PinochleGameManager.TRUMP_MARRIAGE : SpadesMarriage() * PinochleGameManager.MARRIAGE;
            int hearts = Trump == "H" ? HeartsMarriage() * PinochleGameManager.TRUMP_MARRIAGE : HeartsMarriage() * PinochleGameManager.MARRIAGE;
            return AcesAround(true) + KingsAround(true) + QueensAround(true) + JacksAround(true) + Runs(true) + Pinochle(true) + 
                (Nines() * PinochleGameManager.TRUMP_NINE) + clubs + diamonds + spades + hearts;
        }

        public int Nines()
        {
            return Cards.Where(c => c.Suit == Trump && c.Rank == "9").Count();
        }

        public int Runs(bool points=false)
        {
            if (Cards.Where(c => c.Suit == Trump && c.Rank != "9").Count() == (PinochleGameManager.NUM_RANKS * 2) - 2)
            {
                return points ? PinochleGameManager.TRUMP_RUN * PinochleGameManager.DOUBLE_AROUND_MULTIPLIER : 2;
            }
            else if (Cards.Where(c => c.Suit == Trump && c.Rank != "9").Select(c => c.Rank).Distinct().Count() == PinochleGameManager.NUM_RANKS - 1)
            {
                return points ? PinochleGameManager.TRUMP_RUN : 1;
            }
            else
            {
                return 0;
            }
        }

        public int Pinochle(bool points=false)
        {
            int numJacks = Cards.Where(c => c.Rank == "J" && c.Suit == "D").Count();
            int numQueens = Cards.Where(c => c.Rank == "Q" && c.Suit == "S").Count();
            int numPinochle = Pairs(numJacks, numQueens);
            if (points)
            {
                return numPinochle == 2 ? PinochleGameManager.DOUBLE_PINOCHLE : (numPinochle == 1 ? PinochleGameManager.PINOCHLE : 0);
            }
            else
            {
                return numPinochle;
            }
        }

        public int AcesAround(bool points=false)
        {
            return Around("A", points ? PinochleGameManager.ACES_AROUND : 0);
        }

        public int KingsAround(bool points = false)
        {
            return Around("K", points ? PinochleGameManager.KINGS_AROUND : 0);
        }

        public int QueensAround(bool points = false)
        {
            return Around("Q", points ? PinochleGameManager.QUEENS_AROUND : 0);
        }

        public int JacksAround(bool points = false)
        {
            return Around("J", points ? PinochleGameManager.JACKS_AROUND : 0);
        }

        public int ClubsMarriage()
        {
            return Marriage("C");
        }

        public int DiamondsMarriage()
        {
            return Marriage("D");
        }

        public int SpadesMarriage()
        {
            return Marriage("S");
        }

        public int HeartsMarriage()
        {
            return Marriage("H");
        }

        private int Around(string rank, int points)
        {
            if (Cards.Where(c => c.Rank == rank).Count() == PinochleGameManager.NUM_SUITS * 2)
            {
                return points == 0 ? 2 : PinochleGameManager.DOUBLE_AROUND_MULTIPLIER * points;
            }
            else if (Cards.Where(c => c.Rank == rank).Select(c => c.Suit).Distinct().Count() == PinochleGameManager.NUM_SUITS)
            {
                return points == 0 ? 1 : points;
            }
            return 0;
        }

        private int Marriage(string suit)
        {
            int numQueens = Cards.Where(c => c.Rank == "Q" && c.Suit == suit).Count();
            int numKings = Cards.Where(c => c.Rank == "K" && c.Suit == suit).Count();
            int numMarriages =  Pairs(numQueens, numKings);

            // Don't count marriages in runs
            if (suit == Trump)
            {
                int numRuns = Runs();
                numMarriages -= numRuns;
            }

            return numMarriages;
        }

        private int Pairs(int num1, int num2)
        {
            if (num1 == 2 && num2 == 2)
            {
                return 2;
            }
            else if (num1 >= 1 && num2 >= 1)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
