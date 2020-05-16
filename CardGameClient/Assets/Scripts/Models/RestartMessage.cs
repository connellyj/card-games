public class RestartMessage : Message
{
    public string PlayerName;
    public bool NewGame;
    public string Type;

    public RestartMessage(string playerName, bool newGame) : base("Restart")
    {
        PlayerName = playerName;
        NewGame = newGame;
    }

    public override bool IsValid()
    {
        return Type == "Restart";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}
