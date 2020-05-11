using System.Linq;

namespace PinochleServer.Models
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

        public override string GenerateId()
        {
            return string.Join(",", Cards.Select(c => c.ToString()));
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
