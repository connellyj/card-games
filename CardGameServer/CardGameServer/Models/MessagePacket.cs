using System.Collections.Generic;

namespace CardGameServer.Models
{
    public class MessagePacket
    {
        public Message Message { get; set; }
        public IEnumerable<string> SendTo { get; set; }

        public MessagePacket(Message message, IEnumerable<string> sendTo=null)
        {
            Message = message;
            SendTo = sendTo;
        }

        public MessagePacket(Message message, string sendTo) : this(message, new List<string>() { sendTo })
        {
        }
    }
}
