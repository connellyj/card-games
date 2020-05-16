public class DisconnectMessage : Message
{
    public string PlayerName;
    public string Type;

    public DisconnectMessage(string playerName) : base("Disconnect")
    {
        PlayerName = playerName;
    }

    public override bool IsValid()
    {
        return Type == "Disconnect";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}