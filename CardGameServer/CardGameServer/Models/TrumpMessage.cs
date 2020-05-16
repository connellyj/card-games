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

        public override bool IsValid()
        {
            return Type == "Trump";
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
