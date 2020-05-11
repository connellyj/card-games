namespace PinochleServer.Models
{
    public class AvailableGamesMessage : Message
    {
        public string[] AvailableGames;
        public string Type;

        public AvailableGamesMessage(string[] availableGames) : base("AvailableGames")
        {
            AvailableGames = availableGames;
        }

        public override bool IsValid()
        {
            return Type == "AvailableGames";
        }

        public override string GenerateId()
        {
            return string.Join(",", AvailableGames);
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
