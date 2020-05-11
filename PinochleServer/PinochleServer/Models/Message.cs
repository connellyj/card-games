namespace PinochleServer.Models
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
        public abstract string GenerateId();
        protected abstract void SetType(string type);
    }
}
