public class Response : Message
{
    public bool Success;
    public string MessageId;
    public string ErrorMessage;
    public string Type;

    public Response(bool success, string messageId, string errorMessage = null) : base("Response")
    {
        Success = success;
        MessageId = messageId;
        ErrorMessage = errorMessage;
    }

    public override bool IsValid()
    {
        return Type == "Response";
    }

    public override string GenerateId()
    {
        return "response:" + MessageId;
    }

    protected override void SetType(string type)
    {
        Type = type;
    }
}