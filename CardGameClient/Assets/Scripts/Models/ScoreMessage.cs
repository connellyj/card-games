public class ScoreMessage : Message
{
    public string PlayerName;
    public int Score;
    public int ScoreDif;
    public int MissedBy;
    public string Type;

    public ScoreMessage(string playerName, int score, int scoreDif, int missedBy) : base("Score")
    {
        PlayerName = playerName;
        Score = score;
        ScoreDif = scoreDif;
        MissedBy = missedBy;
    }

    public override bool IsValid()
    {
        return Type == "Score";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}