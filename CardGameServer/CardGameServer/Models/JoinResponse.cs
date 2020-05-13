namespace CardGameServer.Models
{
    public class JoinResponse : Message
    {
        public bool Success;
        public string UserName;
        public string ErrorMessage;
        public string Type;

        public JoinResponse(bool success, string userName=null, string errorMessage=null) : base("JoinResponse")
        {
            Success = success;
            UserName = userName;
            ErrorMessage = errorMessage;
        }

        public override bool IsValid()
        {
            return Type == "JoinResponse";
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
