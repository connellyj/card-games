using System;
using System.Collections.Generic;

namespace PinochleServer.Models
{
    public class Card : IComparable<Card>, IEquatable<Card>
    {
        public string Suit;
        public string Rank;

        public static readonly List<string> Suits = new List<string>() { "C", "D", "S", "H" };
        public static readonly List<string> Ranks = new List<string>() { "9", "J", "Q", "K", "10", "A" };

        public Card(string suit, string rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public int CompareTo(Card card)
        {
            return GetSortValue() - card.GetSortValue();
        }

        public override string ToString()
        {
            return Rank + "/" + Suit;
        }

        public int GetSortValue()
        {
            return (Suits.IndexOf(Suit) * 100) + Ranks.IndexOf(Rank);
        }

        public bool Equals(Card other)
        {
            return Suit == other.Suit && Rank == other.Rank;
        }
    }
}
