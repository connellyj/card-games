using System;

namespace CardGameServer.Models
{
    public class Card : IComparable<Card>, IEquatable<Card>
    {
        public string Suit;
        public string Rank;
        public int SortKey;

        public Card(string suit, string rank, int sortKey)
        {
            Suit = suit;
            Rank = rank;
            SortKey = sortKey;
        }

        public int CompareTo(Card card)
        {
            return GetSortValue() - card.GetSortValue();
        }

        public override string ToString()
        {
            return Rank + "/" + Suit;
        }

        public virtual int GetSortValue()
        {
            return SortKey;
        }

        public bool Equals(Card other)
        {
            return Suit == other.Suit && Rank == other.Rank;
        }
    }
}
