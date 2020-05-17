using System;

public class Card : IComparable<Card>, IEquatable<Card>
{
    public string Suit;
    public string Rank;
    public int SortKey;
    public int ReverseSortKey;

    public Card(string suit, string rank, int sortKey, int reverseKey)
    {
        Suit = suit;
        Rank = rank;
        SortKey = sortKey;
        ReverseSortKey = reverseKey;
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
        return SortKey;
    }

    public bool Equals(Card other)
    {
        return Suit == other.Suit && Rank == other.Rank;
    }
}