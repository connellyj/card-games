using CardGameServer.GameManagers;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer.GameMechanics
{
    public class MeldCounter
    {
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

        private readonly IEnumerable<Card> Cards;
        private readonly string Trump;
        private readonly int NumSuits;
        private readonly int NumRanks;

        public MeldCounter(IEnumerable<Card> cards, string trump, int numSuits, int numRanks)
        {
            Cards = cards;
            Trump = trump;
            NumSuits = numSuits;
            NumRanks = numRanks;
        }

        public int TotalMeld()
        {
            int clubs = Trump == "C" ? ClubsMarriage() * TRUMP_MARRIAGE : ClubsMarriage() * MARRIAGE;
            int diamonds = Trump == "D" ? DiamondsMarriage() * TRUMP_MARRIAGE : DiamondsMarriage() * MARRIAGE;
            int spades = Trump == "S" ? SpadesMarriage() * TRUMP_MARRIAGE : SpadesMarriage() * MARRIAGE;
            int hearts = Trump == "H" ? HeartsMarriage() * TRUMP_MARRIAGE : HeartsMarriage() * MARRIAGE;
            return AcesAround(true) + KingsAround(true) + QueensAround(true) + JacksAround(true) + Runs(true) + Pinochle(true) + 
                (Nines() * TRUMP_NINE) + clubs + diamonds + spades + hearts;
        }

        public int Nines()
        {
            return Cards.Where(c => c.Suit == Trump && c.Rank == "9").Count();
        }

        public int Runs(bool points=false)
        {
            if (Cards.Where(c => c.Suit == Trump && c.Rank != "9").Count() == (NumRanks * 2) - 2)
            {
                return points ? TRUMP_RUN * DOUBLE_AROUND_MULTIPLIER : 2;
            }
            else if (Cards.Where(c => c.Suit == Trump && c.Rank != "9").Select(c => c.Rank).Distinct().Count() == NumRanks - 1)
            {
                return points ? TRUMP_RUN : 1;
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
                return numPinochle == 2 ? DOUBLE_PINOCHLE : (numPinochle == 1 ? PINOCHLE : 0);
            }
            else
            {
                return numPinochle;
            }
        }

        public int AcesAround(bool points=false)
        {
            return Around("A", points ? ACES_AROUND : 0);
        }

        public int KingsAround(bool points = false)
        {
            return Around("K", points ? KINGS_AROUND : 0);
        }

        public int QueensAround(bool points = false)
        {
            return Around("Q", points ? QUEENS_AROUND : 0);
        }

        public int JacksAround(bool points = false)
        {
            return Around("J", points ? JACKS_AROUND : 0);
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
            if (Cards.Where(c => c.Rank == rank).Count() == NumSuits * 2)
            {
                return points == 0 ? 2 : DOUBLE_AROUND_MULTIPLIER * points;
            }
            else if (Cards.Where(c => c.Rank == rank).Select(c => c.Suit).Distinct().Count() == NumSuits)
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
