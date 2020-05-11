public class GameInfoMessage : Message
{
    public int NumPlayers;
    public string Type;

    public GameInfoMessage(int numPlayers) : base("GameInfo")
    {
        NumPlayers = numPlayers;
    }

    public override bool IsValid()
    {
        return Type == "GameInfo";
    }

    public override string GenerateId()
    {
        throw new System.NotImplementedException();
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}