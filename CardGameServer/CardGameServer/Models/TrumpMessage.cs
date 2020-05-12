namespace CardGameServer.Models
{
    public class TrumpMessage : Message
    {
        public string TrumpSuit;
        public string ChoosingPlayer;
        public string Type;

        public TrumpMessage(string choosingPlayer, string trump = "") : base("Trump")
        {
            ChoosingPlayer = choosingPlayer;
            TrumpSuit = trump;
        }

        public override string ToString()
        {
            return TrumpSuit == string.Empty ? "Choosing trump..." : "Trump is: " + TrumpSuit;
        }

        public override bool IsValid()
        {
            return Type == "Trump";
        }

        public override string GenerateId()
        {
            return ChoosingPlayer + ":" + TrumpSuit;
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
