using System.Collections.Generic;
using System.Linq;

namespace CardGameServer.GameMechanics
{
    public class HeartsTrickDecider : TrickDecider
    {
        public bool ArePointsBroken { get; set; }

        public HeartsTrickDecider() : base()
        {
            ArePointsBroken = false;
        }

        public override List<Card> TrickStartValidCards(List<Card> hand)
        {
            if (ArePointsBroken)
            {
                return hand;
            }
            else
            {
                return hand.Where(c => c.Suit != "H" && !(c.Suit == "S" && c.Rank == "Q")).ToList();
            }
        }

        public override List<Card> FirstTrickStartValidCards(List<Card> hand)
        {
            return hand.Where(c => c.Rank == "2" && c.Suit == "C").ToList();
        }

        public override List<Card> FirstTrickValidCards(List<Card> hand, List<Card> trick)
        {
            List<Card> orig = ValidCards(hand, trick);
            List<Card> noHearts = orig.Where(c => c.Suit != "H" && !(c.Suit == "S" && c.Rank == "Q")).ToList();
            if (noHearts.Count > 0)
            {
                return noHearts;
            }
            else
            {
                return orig;
            }
        }
    }
}
