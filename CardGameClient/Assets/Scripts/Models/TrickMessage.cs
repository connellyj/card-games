class TrickMessage : Message
{
    public string WinningPlayer;
    public string Type;

    public TrickMessage(string playerName) : base("Trick")
    {
        WinningPlayer = playerName;
    }

    public override bool IsValid()
    {
        return Type == "Trick";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}