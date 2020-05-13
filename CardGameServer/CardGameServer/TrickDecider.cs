using CardGameServer.Models;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServer
{
    public static class TrickDecider
    {
        public static bool IsPoint(Card c)
        {
            return c.Rank == "10" || c.Rank == "K" || c.Rank == "A";
        }

        public static List<Card> ValidCards(List<Card> hand, List<Card> trick, string trump)
        {
            string followSuit = trick[0].Suit;
            List<Card> followSuitCards = hand.Where(c => c.Suit == followSuit).ToList();
            List<Card> trumpCards = hand.Where(c => c.Suit == trump).ToList();
            Card highestTrumpCard = HighestInSuit(trick, trump);
            if (followSuitCards.Count > 0)
            {
                if (followSuit == trump || highestTrumpCard == null)
                {
                    Card highestCard = HighestInSuit(trick, followSuit);
                    List<Card> higherCards = HigherCards(followSuitCards, highestCard);
                    if (higherCards.Count > 0)
                    {
                        return higherCards;
                    }
                }
                return followSuitCards;
            }
            if (trumpCards.Count > 0)
            {
                if (highestTrumpCard != null)
                {
                    List<Card> higherCards = HigherCards(trumpCards, highestTrumpCard);
                    if (higherCards.Count > 0)
                    {
                        return higherCards;
                    }
                    else
                    {
                        return hand;
                    }
                }
                return trumpCards;
            }
            return hand;
        }

        public static int WinningCard(List<Card> trick, string trump)
        {
            List<Card> trumpCards = trick.Where(c => c.Suit == trump).ToList();
            Card card = null;
            if (trumpCards.Count > 0)
            {
                card = HighestInSuit(trick, trump);
            }
            else
            {
                card = HighestInSuit(trick, trick[0].Suit);
            }
            return trick.IndexOf(card);
        }

        private static List<Card> HigherCards(List<Card> cards, Card highestCard)
        {
            return cards.Where(c => c.CompareTo(highestCard) > 0).ToList();
        }

        private static Card HighestInSuit(List<Card> trick, string suit)
        {
            return trick.Where(c => c.Suit == suit).OrderBy(c => c).LastOrDefault();
        }
    }
}
