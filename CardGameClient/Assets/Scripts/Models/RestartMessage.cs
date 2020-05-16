public class RestartMessage : Message
{
    public string PlayerName;
    public string Type;

    public RestartMessage(string playerName) : base("Restart")
    {
        PlayerName = playerName;
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
