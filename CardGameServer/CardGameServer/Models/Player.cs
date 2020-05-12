using System.Collections.Generic;

namespace CardGameServer.Models
{
    public class Player
    {
        public string GameName;
        public string Name;
        public string Uid;
        public List<Card> Cards;
        public int Score;
        public int SecretScore;
        public int MeldScore;
        public int Order;
        public bool TookATrick;

        public Player(string gameName, string name, string uid, int order)
        {
            GameName = gameName;
            Name = name;
            Uid = uid;
            Order = order;
            Score = 0;
            SecretScore = 0;
            MeldScore = 0;
            TookATrick = false;
        }
    }
}
