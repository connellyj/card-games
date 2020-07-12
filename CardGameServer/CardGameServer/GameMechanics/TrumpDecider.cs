using System.Collections.Generic;
using System.Linq;

namespace CardGameServer.GameMechanics
{
    public class TrumpDecider : TrickDecider
    {
        public string Trump { get; set; }

        public TrumpDecider() : base()
        {
            Trump = string.Empty;
        }

        public override int DecideTrick(List<Card> trick)
        {
            string suit = trick[0].Suit;
            Card highestTrump = trick.Where(c => c.Suit == Trump).OrderBy(c => c.SortKey).LastOrDefault();
            if (highestTrump != null)
            {
                return trick.IndexOf(highestTrump);
            }
            else
            {
                return trick.IndexOf(trick.Where(c => c.Suit == suit).OrderBy(c => c.SortKey).Last());
            }
        }
    }
}
