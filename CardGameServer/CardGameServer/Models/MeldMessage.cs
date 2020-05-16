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
        public int TotalPoints;

        public string PlayerName;
        public string Trump;
        public string Type;

        public MeldMessage(string playerName, string trump, int totalPoints) : base("Meld")
        {
            PlayerName = playerName;
            Trump = trump;
            TotalPoints = totalPoints;
        }

        public override bool IsValid()
        {
            return Type == "Meld";
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
