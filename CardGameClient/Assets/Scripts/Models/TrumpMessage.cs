public class TrumpMessage : Message
{
    public string TrumpSuit;
    public string ChoosingPlayer;
    public string[] UnavailableOptions;
    public string[] ExtraOptions;
    public string Type;

    public TrumpMessage(string choosingPlayer, string trump="", string[] unavailableOptions=null, string[] extraOptions=null) : base("Trump")
    {
        ChoosingPlayer = choosingPlayer;
        TrumpSuit = trump;
        UnavailableOptions = unavailableOptions;
        ExtraOptions = extraOptions;
    }

    public override bool IsValid()
    {
        return Type == "Trump";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}