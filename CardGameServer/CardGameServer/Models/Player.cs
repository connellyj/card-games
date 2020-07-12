using System.Collections.Generic;

namespace CardGameServer.Models
{
    public class Player
    {
        public string Name;
        public string Uid;
        public List<string> TrumpUsed;
        public List<Card> Cards;
        public int Score;
        public int SecretScore;
        public int MeldScore;
        public int OldScore;
        public int MissedBidBy;
        public int Order;
        public int TricksTaken;

        public Player(string name, string uid, int order)
        {
            Name = name;
            Uid = uid;
            Order = order;
            ResetScores();
        }

        public Player(Player p)
        {
            Name = p.Name;
            Uid = p.Uid;
            TrumpUsed = p.TrumpUsed;
            Cards = p.Cards;
            Score = p.Score;
            SecretScore = p.SecretScore;
            MeldScore = p.MeldScore;
            OldScore = p.OldScore;
            MissedBidBy = p.MissedBidBy;
            Order = p.Order;
            TricksTaken = p.TricksTaken;
        }

        public void ResetPerHandScores()
        {
            SecretScore = 0;
            MeldScore = 0;
            OldScore = 0;
            MissedBidBy = 0;
            TricksTaken = 0;
        }

        public void ResetScores()
        {
            TrumpUsed = new List<string>();
            Score = 0;
            ResetPerHandScores();
        }
    }
}
