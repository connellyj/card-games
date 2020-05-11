namespace PinochleServer.Models
{
    public class GameOverMessage : Message
    {
        public string WinningPlayer;
        public string Type;

        public GameOverMessage(string player) : base("GameOver")
        {
            WinningPlayer = player;
        }

        public override bool IsValid()
        {
            return Type == "GameOver";
        }

        public override string GenerateId()
        {
            return "gameover:" + WinningPlayer;
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
