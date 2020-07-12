using System.Collections.Generic;
using System.Linq;

namespace CardGameServer.GameMechanics
{
    public class PinochleTrickDecider : TrumpDecider
    {
        public PinochleTrickDecider()
        {
        }

        public override List<Card> ValidCards(List<Card> hand, List<Card> trick)
        {
            string followSuit = trick[0].Suit;
            List<Card> followSuitCards = hand.Where(c => c.Suit == followSuit).ToList();
            List<Card> trumpCards = hand.Where(c => c.Suit == Trump).ToList();
            Card highestTrumpCard = HighestInSuit(trick, Trump);
            if (followSuitCards.Count > 0)
            {
                if (followSuit == Trump || highestTrumpCard == null)
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
                }
                return trumpCards;
            }
            return hand;
        }

        public static bool IsPoint(Card c)
        {
            return c.Rank == "10" || c.Rank == "K" || c.Rank == "A";
        }

        private static List<Card> HigherCards(List<Card> cards, Card highestCard)
        {
            return cards.Where(c => c.CompareTo(highestCard) > 0).ToList();
        }

        private static Card HighestInSuit(List<Card> trick, string suit)
        {
            return trick.Where(c => c.Suit == suit).OrderBy(c => c.SortKey).LastOrDefault();
        }
    }
}
