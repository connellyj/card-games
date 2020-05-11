namespace PinochleServer.Models
{
    public class ScoreMessage : Message
    {
        public string PlayerName;
        public int Score;
        public string Type;

        public ScoreMessage(string playerName, int score) : base("Score")
        {
            PlayerName = playerName;
            Score = score;
        }

        public override bool IsValid()
        {
            return Type == "Score";
        }

        public override string GenerateId()
        {
            return PlayerName + ":" + Score;
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
