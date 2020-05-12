public class GameTypeMessage : Message
{
    public string[] GameTypes;
    public string ChosenGame;
    public string Type;

    public GameTypeMessage(string[] gameTypes=null, string game = "") : base("GameType")
    {
        GameTypes = gameTypes;
        ChosenGame = game;
    }

    public override bool IsValid()
    {
        return Type == "GameType";
    }

    public override string GenerateId()
    {
        return "gametype:" + ChosenGame;
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}