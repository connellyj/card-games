using CardGameServer.GameManagers;
using CardGameServer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CardGameServerTests
{
    public class GamesManagerTest
    {
        [TestMethod]
        public void TestHandleNewConnection()
        {
            string uid = "uid";
            MessagePackets messages = GamesManager.Get().HandleNewConnection(uid);

            Assert.IsTrue(Utils.VerifyMessagePackets(messages, typeof(GameTypeMessage)));

            Assert.AreEqual(messages.Messages[0].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[0].SendTo.Single(), uid);

            GameTypeMessage gt = (GameTypeMessage)messages.Messages[0].Message;
            CollectionAssert.AreEqual(gt.GameTypes, new string[3] { "Hearts", "Mizerka", "Pinochle" });
            Assert.AreEqual(gt.ChosenGame, null);
        }

        [DataTestMethod]
        [DataRow("Hearts", typeof(AvailableGamesMessage))]
        [DataRow("Mizerka", typeof(AvailableGamesMessage))]
        [DataRow("Pinochle", typeof(AvailableGamesMessage))]
        [DataRow("Nonsense")]
        public void TestHandleGameTypes(string chosenGame, params Type[] messageTypes)
        {
            // TODO: check values in messages
            // TODO: check game state afterwards (need to expose static game state info)
            string uid = "uid";
            GameTypeMessage gt = new GameTypeMessage() { ChosenGame = chosenGame };

            if (messageTypes.Length > 0)
            {
                MessagePackets messages = GamesManager.Get().HandleGameTypes(uid, gt);

                Assert.IsTrue(Utils.VerifyMessagePackets(messages, messageTypes));

                foreach (MessagePacket m in messages.Messages)
                {
                    Assert.AreEqual(m.SendTo.Count(), 1);
                    Assert.AreEqual(m.SendTo.Single(), uid);
                }
            }
            else
            {
                Assert.ThrowsException<Exception>(() => GamesManager.Get().HandleGameTypes(uid, gt));
            }
        }
    }
}
