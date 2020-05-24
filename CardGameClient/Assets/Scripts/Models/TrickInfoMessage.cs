using System.Collections.Generic;

public class TrickInfoMessage : Message
{
    public Dictionary<string, int> TricksLeft;
    public string Type;

    public TrickInfoMessage(Dictionary<string, int> tricksLeft) : base("TrickInfo")
    {
        TricksLeft = tricksLeft;
    }

    public override bool IsValid()
    {
        return Type == "TrickInfo";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}
