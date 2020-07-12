using System.Collections.Generic;
using System.Linq;

namespace CardGameServer.GameMechanics
{
    public class TrickDecider
    {
        public TrickDecider()
        {
        }

        public virtual List<Card> TrickStartValidCards(List<Card> hand)
        {
            return hand;
        }

        public virtual List<Card> FirstTrickStartValidCards(List<Card> hand)
        {
            return TrickStartValidCards(hand);
        }

        public virtual List<Card> FirstTrickValidCards(List<Card> hand, List<Card> trick)
        {
            return ValidCards(hand, trick);
        }

        public virtual List<Card> ValidCards(List<Card> hand, List<Card> trick)
        {
            string suit = trick[0].Suit;
            List<Card> followSuit = hand.Where(c => c.Suit == suit).ToList();
            if (followSuit.Count > 0)
            {
                return followSuit;
            }
            else
            {
                return hand;
            }
        }

        public virtual int DecideTrick(List<Card> trick)
        {
            string suit = trick[0].Suit;
            return trick.IndexOf(trick.Where(c => c.Suit == suit).OrderBy(c => c.SortKey).Last());
        }
    }
}
