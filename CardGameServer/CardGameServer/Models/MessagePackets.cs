using System.Collections.Generic;

namespace CardGameServer.Models
{
    public class MessagePackets
    {
        public List<MessagePacket> Messages;

        public MessagePackets()
        {
            Messages = new List<MessagePacket>();
        }

        public MessagePackets(Message message, string sendTo)
        {
            Messages = new List<MessagePacket>();
            Add(message, sendTo);
        }

        public MessagePackets(Message message, IEnumerable<string> sendTo=null)
        {
            Messages = new List<MessagePacket>();
            Add(message, sendTo);
        }

        public MessagePackets(MessagePacket message)
        {
            Messages = new List<MessagePacket>();
            Add(message);
        }

        public void Add(Message message, IEnumerable<string> sendTo=null)
        {
            Messages.Add(new MessagePacket(message, sendTo));
        }

        public void Add(Message message, string sendTo)
        {
            Messages.Add(new MessagePacket(message, sendTo));
        }

        public void Add(MessagePackets messages)
        {
            Messages.AddRange(messages.Messages);
        }

        public void Add(MessagePacket message)
        {
            Messages.Add(message);
        }
    }
}
