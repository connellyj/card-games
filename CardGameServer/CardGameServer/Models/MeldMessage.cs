using System.Collections.Generic;

namespace CardGameServer.Models
{
    public class MeldMessage : Message
    { 
        public int AcesAround;
        public int KingsAround;
        public int QueensAround;
        public int JacksAround;
        public int ClubsMarriage;
        public int DiamondsMarriage;
        public int SpadesMarriage;
        public int HeartsMarriage;
        public int Pinochle;
        public int TrumpNine;
        public int Run;

        public string PlayerName;
        public string Trump;
        public string Type;

        public MeldMessage(string playerName, string trump) : base("Meld")
        {
            PlayerName = playerName;
            Trump = trump;
        }

        public override string ToString()
        {
            List<string> meld = new List<string>();
            if (AcesAround > 0) meld.Add("Aces Around x" + AcesAround.ToString());
            if (KingsAround > 0) meld.Add("Kings Around x" + KingsAround.ToString());
            if (QueensAround > 0) meld.Add("Queens Around x" + QueensAround.ToString());
            if (JacksAround > 0) meld.Add("Jacks Around x" + JacksAround.ToString());
            if (ClubsMarriage > 0) meld.Add("Marriage in Clubs x" + ClubsMarriage.ToString());
            if (DiamondsMarriage > 0) meld.Add("Marriage in Diamonds x" + DiamondsMarriage.ToString());
            if (SpadesMarriage > 0) meld.Add("Marriage in Spades x" + SpadesMarriage.ToString());
            if (HeartsMarriage > 0) meld.Add("Marriage in Hearts x" + HeartsMarriage.ToString());
            if (Pinochle > 0) meld.Add("Pinochle x" + Pinochle.ToString());
            if (Run > 0) meld.Add("Run x" + Run.ToString());
            if (TrumpNine > 0) meld.Add("9 x" + TrumpNine.ToString());
            string meldStr = meld.Count > 0 ? string.Join(", ", meld) : "None :(";
            return "Meld: " + meldStr;
        }

        public override bool IsValid()
        {
            return Type == "Meld";
        }

        public override string GenerateId()
        {
            return "meld:" + Trump;
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
