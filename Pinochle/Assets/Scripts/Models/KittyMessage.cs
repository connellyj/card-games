using System.Linq;

public class KittyMessage : Message
{
    public Card[] Kitty;
    public string ChoosingPlayer;
    public string Type;

    public KittyMessage(Card[] kitty, string choosingPlayer) : base("Kitty")
    {
        Kitty = kitty;
        ChoosingPlayer = choosingPlayer;
    }

    public override string ToString()
    {
        return string.Format("Choosing {0} cards to discard...", Kitty.Length);
    }

    public override bool IsValid()
    {
        return Type == "Kitty";
    }

    public override string GenerateId()
    {
        return ChoosingPlayer + string.Join(",", Kitty.Select(c => c.ToString()));
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}