namespace CardGameServer.Models
{
    public abstract class Message
    {
        protected Message()
        {
        }

        protected Message(string type)
        {
            SetType(type);
        }

        public abstract bool IsValid();
        protected abstract void SetType(string type);
    }
}
