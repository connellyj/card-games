public class BidMessage : Message
{
    public string PlayerName;
    public int Bid;
    public int CurBid;
    public string Type;

    public BidMessage(string playerName, int curBid = -1, int bid = -1) : base("Bid")
    {
        PlayerName = playerName;
        CurBid = curBid;
        Bid = bid;
    }

    public override string ToString()
    {
        return Bid == 0 ? "Pass" : string.Format("Bid {0}", Bid.ToString());
    }

    public override bool IsValid()
    {
        return Type == "Bid";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}