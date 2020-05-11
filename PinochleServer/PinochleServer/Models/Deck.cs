using System;
using System.Collections.Generic;

namespace PinochleServer.Models
{
    public static class Deck
    {
        public static Card[] Shuffle()
        {
            List<Card> cards = new List<Card>();
            foreach (string s in Card.Suits)
            {
                foreach (string r in Card.Ranks)
                {
                    Card card = new Card(s, r);
                    cards.Add(card);
                    cards.Add(card);
                }
            }

            Random rnd = new Random();
            Card[] shuffledCards = new Card[cards.Count];
            for (int i = 0; i < shuffledCards.Length; i++)
            {
                Card c = cards[rnd.Next(0, cards.Count)];
                cards.Remove(c);
                shuffledCards[i] = c;
            }
            return shuffledCards;
        }
    }
}
