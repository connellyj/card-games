namespace CardGameServer.Models
{
    public class MeldPointsMessage : Message
    {
        public int AcesAroundPoints;
        public int KingsAroundPoints;
        public int QueensAroundPoints;
        public int JacksAroundPoints;
        public int AroundMultiplierPoints;
        public int MarriagePoints;
        public int TrumpMarriagePoints;
        public int PinochlePoints;
        public int DoublePinochlePoints;
        public int TrumpNinePoints;
        public int RunPoints;
        public string Type;

        public MeldPointsMessage(int acesAroundPoints, int kingsAroundPoints, int queensAroundPoints, int jacksAroundPoints,
            int aroundMultipler, int marriagePoints, int trumpMarriagePoints, int pinochlePoints, int doublePinochlePoints,
            int trumpNinePoints, int runPoints) : base ("MeldPoints")
        {
            AcesAroundPoints = acesAroundPoints;
            KingsAroundPoints = kingsAroundPoints;
            QueensAroundPoints = queensAroundPoints;
            JacksAroundPoints = jacksAroundPoints;
            AroundMultiplierPoints = aroundMultipler;
            MarriagePoints = marriagePoints;
            TrumpMarriagePoints = trumpMarriagePoints;
            PinochlePoints = pinochlePoints;
            DoublePinochlePoints = doublePinochlePoints;
            TrumpNinePoints = trumpNinePoints;
            RunPoints = runPoints;
        }

        public override bool IsValid()
        {
            return Type == "MeldPoints";
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
