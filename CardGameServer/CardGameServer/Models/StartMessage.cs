using System.Linq;

namespace CardGameServer.Models
{
    public class StartMessage : Message
    {
        public Card[] Cards;
        public string Type;

        public StartMessage(Card[] cards) : base("Start")
        {
            Cards = cards;
        }

        public override bool IsValid()
        {
            return Type == "Start";
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
