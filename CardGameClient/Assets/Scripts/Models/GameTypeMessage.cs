public class GameTypeMessage : Message
{
    public string[] GameTypes;
    public string ChosenGame;
    public string Type;

    public GameTypeMessage(string[] gameTypes=null, string game=null) : base("GameType")
    {
        GameTypes = gameTypes;
        ChosenGame = game;
    }

    public override bool IsValid()
    {
        return Type == "GameType";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}