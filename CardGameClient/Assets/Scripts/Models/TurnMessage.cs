public class TurnMessage : Message
{
    public string PlayerName;
    public Card[] ValidCards;
    public Card Card;
    public bool IsFirstCard;
    public string Type;

    public TurnMessage(string playerName, Card[] validCards = null, Card card = null, bool isFirstCard = false) : base("Turn")
    {
        PlayerName = playerName;
        ValidCards = validCards;
        IsFirstCard = isFirstCard;
        Card = card;
    }

    public override bool IsValid()
    {
        return Type == "Turn";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}