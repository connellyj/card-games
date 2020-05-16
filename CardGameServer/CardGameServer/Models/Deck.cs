using System;
using System.Collections.Generic;

namespace CardGameServer.Models
{
    public static class Deck
    {
        public static Card[] Shuffle(List<string> suits, List<string> ranks)
        {
            List<Card> cards = new List<Card>();
            foreach (string s in suits)
            {
                foreach (string r in ranks)
                {
                    Card card = new Card(s, r, (suits.IndexOf(s) * 100) + ranks.IndexOf(r), (suits.IndexOf(s) * 100) - ranks.IndexOf(r));
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
