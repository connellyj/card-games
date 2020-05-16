public class SettingsMessage : Message
{
    public bool SortReverse;
    public string Type;

    public SettingsMessage(bool reverse) : base("Settings")
    {
        SortReverse = reverse;
    }

    public override bool IsValid()
    {
        return Type == "Settings";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}
