using CardGameServer.Models;
using System;

namespace CardGameServerTests
{
    public class Utils
    {
        public static bool VerifyMessagePackets(MessagePackets messages, params Type[] messageTypes)
        {
            if (messages.Messages.Count != messageTypes.Length)
            {
                return false;
            }
            for (int i = 0; i < messages.Messages.Count; i++)
            {
                if(messages.Messages[i].Message.GetType() != messageTypes[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
