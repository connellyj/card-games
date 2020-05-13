namespace CardGameServer.Models
{
    public class JoinMessage : Message
    {
        public string GameName;
        public string UserName;
        public int Order;
        public string Type;

        public JoinMessage(string userName, string gameName=null, int order=0) : base("Join")
        {
            UserName = userName;
            GameName = gameName;
            Order = order;
        }

        public override string ToString()
        {
            return "Joined game '" + GameName + "'";
        }

        public override bool IsValid()
        {
            return Type == "Join";
        }

        protected override void SetType(string type)
        {
            Type = type;
        }
    }
}
