public class PassMessage : Message
{
    public string PassingPlayer;
    public string PassingTo;
    public int NumToPass;
    public Card[] PassedCards;
    public string Type;

    public PassMessage(string player, int numToPass=0, string passingTo = "", Card[] cards = null) : base("Pass")
    {
        PassingPlayer = player;
        PassingTo = passingTo;
        NumToPass = numToPass;
        PassedCards = cards;
    }

    public override bool IsValid()
    {
        return Type == "Pass";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}