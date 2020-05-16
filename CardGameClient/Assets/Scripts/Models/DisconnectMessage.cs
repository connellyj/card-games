public class DisconnectMessage : Message
{
    public string PlayerName;
    public bool ShouldDisableGame;
    public string Type;

    public DisconnectMessage(string playerName, bool disableGame) : base("Disconnect")
    {
        PlayerName = playerName;
        ShouldDisableGame = disableGame;
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