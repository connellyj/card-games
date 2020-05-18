public class ErrorResponse : Message
{
    public string ErrorMessage;
    public string Type;

    public ErrorResponse(string errorMessage) : base("ErrorResponse")
    {
        ErrorMessage = errorMessage;
    }

    public override bool IsValid()
    {
        return Type == "ErrorResponse";
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}
