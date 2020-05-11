public class TurnMessage : Message
{
    public string PlayerName;
    public Card[] ValidCards;
    public Card Card;
    public string Type;

    public TurnMessage(string playerName, Card[] validCards = null, Card card = null) : base("Turn")
    {
        PlayerName = playerName;
        ValidCards = validCards;
        Card = card;
    }

    public override bool IsValid()
    {
        return Type == "Turn";
    }

    public override string GenerateId()
    {
        return "turn:" + (Card == null ? "" : Card.ToString());
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}