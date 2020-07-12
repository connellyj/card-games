using CardGameServer.GameManagers;
using CardGameServer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGameServerTests
{
    [TestClass]
    public class GameManagerTest
    {
        private const string ExistingUid = "existingUid";
        private const string ExistingUserName = "existingUserName";
        private const string ExistingGameName = "existingGameName";

        private GameManager gm;

        [TestInitialize]
        public void TestSetUp()
        {
            gm = new GameManager();
        }

        [TestMethod]
        public void TestHandleJoin()
        {
            string uid = "uid";
            string userName = "name";
            string gameName = "game";
            MessagePackets messages = JoinGame(uid, userName, gameName);

            List<Player> players = gm.GetPlayers();
            Assert.AreEqual(players.Count, 1);
            Assert.AreEqual(players.Last().Name, userName);
            Assert.AreEqual(players.Last().Uid, uid);

            Assert.IsTrue(Utils.VerifyMessagePackets(messages, typeof(JoinResponse), typeof(JoinMessage)));

            JoinResponse jr = (JoinResponse)messages.Messages[0].Message;
            Assert.AreEqual(messages.Messages[0].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[0].SendTo.Single(), uid);
            Assert.AreEqual(jr.UserName, userName);
            Assert.AreEqual(jr.ErrorMessage, null);
            Assert.IsTrue(jr.Success);

            JoinMessage jm = (JoinMessage)messages.Messages[1].Message;
            Assert.AreEqual(messages.Messages[1].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[1].SendTo.Single(), uid);
            Assert.AreEqual(jm.UserName, userName);
            Assert.AreEqual(jm.GameName, gameName);
            Assert.AreEqual(jm.Order, 0);
        }

        [TestMethod]
        public void TestHandleJoinExistingGame()
        {
            string uid = "uid";
            string userName = "name";
            CreateExistingGame();
            MessagePackets messages = JoinGame(uid, userName, ExistingGameName);

            List<Player> players = gm.GetPlayers();
            Assert.AreEqual(players.Count, 2);
            Assert.AreEqual(players.Last().Name, userName);
            Assert.AreEqual(players.Last().Uid, uid);

            Assert.IsTrue(Utils.VerifyMessagePackets(messages, typeof(JoinResponse), typeof(JoinMessage), typeof(JoinMessage)));

            JoinResponse jr = (JoinResponse)messages.Messages[0].Message;
            Assert.AreEqual(messages.Messages[0].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[0].SendTo.Single(), uid);
            Assert.AreEqual(jr.UserName, userName);
            Assert.AreEqual(jr.ErrorMessage, null);
            Assert.IsTrue(jr.Success);

            JoinMessage jm1 = (JoinMessage)messages.Messages[1].Message;
            Assert.AreEqual(messages.Messages[1].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[1].SendTo.Single(), uid);
            Assert.AreEqual(jm1.UserName, ExistingUserName);
            Assert.AreEqual(jm1.GameName, ExistingGameName);
            Assert.AreEqual(jm1.Order, 0);

            JoinMessage jm2 = (JoinMessage)messages.Messages[2].Message;
            Assert.AreEqual(messages.Messages[2].SendTo.Count(), 2);
            CollectionAssert.AreEqual(messages.Messages[2].SendTo.ToList(), new List<string>() { ExistingUid, uid });
            Assert.AreEqual(jm2.UserName, userName);
            Assert.AreEqual(jm2.GameName, ExistingGameName);
            Assert.AreEqual(jm2.Order, 1);
        }

        [TestMethod]
        public void TestHandleJoinGameStart()
        {
            string uid = "uid";
            string gameName = "fullGame";
            List<string> uids = new List<string>();
            MessagePackets messages = new MessagePackets();
            for (int i = 0; i < 4; i++)
            {
                string num = i.ToString();
                uids.Add(uid + num);
                messages = JoinGame(uid + num, num, gameName);
            }

            Assert.IsTrue(gm.HasStarted());
            Assert.AreEqual(gm.GetCurrentPlayerIndex(), 1);
            Assert.AreEqual(gm.GetDealerIndex(), 0);

            Assert.IsTrue(Utils.VerifyMessagePackets(messages, typeof(JoinResponse), 
                typeof(JoinMessage), typeof(JoinMessage), typeof(JoinMessage), typeof(JoinMessage), 
                typeof(StartMessage), typeof(StartMessage), typeof(StartMessage), typeof(StartMessage), typeof(TurnMessage)));

            List<Player> players = gm.GetPlayers();
            for (int i = 5; i < 9; i++)
            {
                Assert.AreEqual(messages.Messages[i].SendTo.Single(), uid + (i - 5).ToString());
                StartMessage sm = (StartMessage)messages.Messages[i].Message;
                Assert.AreEqual(sm.Cards.Length, 13);
                CollectionAssert.AreEqual(sm.Cards, players[i - 5].Cards);
            }
            CollectionAssert.AreEqual(messages.Messages[9].SendTo.ToList(), uids);
            TurnMessage tm = (TurnMessage)messages.Messages[9].Message;
            Assert.AreEqual(tm.PlayerName, "1");
            CollectionAssert.AreEqual(tm.ValidCards, ((StartMessage)messages.Messages[6].Message).Cards);
            Assert.IsTrue(tm.IsFirstCard);
        }

        [TestMethod]
        public void TestHandleJoinDuplicateUserName()
        {
            string uid = "uid";
            CreateExistingGame();
            Assert.AreEqual(gm.GetPlayers().Count, 1);
            MessagePackets messages = JoinGame(uid, ExistingUserName, ExistingGameName);

            Assert.AreEqual(gm.GetPlayers().Count, 1);

            Assert.IsTrue(Utils.VerifyMessagePackets(messages, typeof(JoinResponse)));

            JoinResponse jr = (JoinResponse)messages.Messages[0].Message;
            Assert.AreEqual(messages.Messages[0].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[0].SendTo.Single(), uid);
            Assert.AreEqual(jr.UserName, null);
            Assert.AreNotEqual(jr.ErrorMessage, null);
            Assert.IsFalse(jr.Success);
        }

        [TestMethod]
        public void TestHandleJoinFullGame()
        {
            string gameName = "fullGame";
            JoinGame("uid1", "1", gameName);
            JoinGame("uid2", "2", gameName);
            JoinGame("uid3", "3", gameName);
            JoinGame("uid4", "4", gameName);
            Assert.ThrowsException<Exception>(() => JoinGame("uid", "name", gameName));
        }

        [TestMethod]
        public void TestHandleJoinGameOver()
        {
            string uid = "uid";
            string gameName = "fullGame";
            JoinGame("uid1", "1", gameName);
            JoinGame("uid2", "2", gameName);
            JoinGame("uid3", "3", gameName);
            JoinGame("uid4", "4", gameName);
            gm.HandleDisconnect("uid1");
            Assert.ThrowsException<Exception>(() => JoinGame(uid, "name", gameName));
        }

        [TestMethod]
        public void TestHandleDisconnectNotStarted()
        {
            string uid = "uid";
            string userName = "name";
            CreateExistingGame();
            JoinGame(uid, userName, ExistingGameName);
            MessagePackets messages = gm.HandleDisconnect(uid);

            List<Player> players = gm.GetPlayers();
            Assert.AreEqual(players.Count, 1);
            CollectionAssert.DoesNotContain(players.Select(p => p.Uid).ToList(), uid);
            Assert.IsFalse(gm.HasEnded());

            Assert.IsTrue(Utils.VerifyMessagePackets(messages, typeof(DisconnectMessage)));

            DisconnectMessage d = (DisconnectMessage)messages.Messages[0].Message;
            Assert.AreEqual(messages.Messages[0].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[0].SendTo.Single(), ExistingUid);
            Assert.AreEqual(d.PlayerName, userName);
            Assert.IsFalse(d.ShouldDisableGame);
        }

        [TestMethod]
        public void TestHandleDisconnectGameOver()
        {
            string uid = "uid";
            string userName = "name";
            CreateExistingGame();
            JoinGame("tmp", "tmp", ExistingGameName);
            JoinGame("tmp2", "tmp2", ExistingGameName);
            JoinGame(uid, userName, ExistingGameName);
            gm.HandleDisconnect("tmp");
            MessagePackets messages = gm.HandleDisconnect(uid);

            Assert.AreEqual(gm.GetPlayers().Count, 2);
            Assert.IsTrue(gm.HasEnded());

            Assert.IsTrue(Utils.VerifyMessagePackets(messages, typeof(DisconnectMessage)));

            DisconnectMessage d = (DisconnectMessage)messages.Messages[0].Message;
            Assert.AreEqual(messages.Messages[0].SendTo.Count(), 2);
            CollectionAssert.AreEqual(messages.Messages[0].SendTo.ToList(), new List<string>() { ExistingUid, "tmp2" });
            Assert.AreEqual(d.PlayerName, userName);
            Assert.IsFalse(d.ShouldDisableGame);
        }

        [TestMethod]
        public void HandleDisconnectStarted()
        {
            string uid = "uid";
            string userName = "name";
            string gameName = "fullGame";
            JoinGame(uid, userName, gameName);
            JoinGame("uid2", "2", gameName);
            JoinGame("uid3", "3", gameName);
            JoinGame("uid4", "4", gameName);
            MessagePackets messages = gm.HandleDisconnect(uid);

            Assert.AreEqual(gm.GetPlayers().Count, 3); 
            Assert.IsTrue(gm.HasEnded());

            Assert.IsTrue(Utils.VerifyMessagePackets(messages, typeof(DisconnectMessage)));

            DisconnectMessage d = (DisconnectMessage)messages.Messages[0].Message;
            Assert.AreEqual(messages.Messages[0].SendTo.Count(), 3);
            CollectionAssert.AreEqual(messages.Messages[0].SendTo.ToList(), new List<string>() { "uid2", "uid3", "uid4" });
            Assert.AreEqual(d.PlayerName, userName);
            Assert.IsTrue(d.ShouldDisableGame);
        }

        [TestMethod]
        public void TestHandleDisconnectOutOfOrder()
        {
            Assert.ThrowsException<Exception>(() => gm.HandleDisconnect("uid"));
        }

        [TestMethod]
        public void TestHandleRestart()
        {
            string uid = "uid";
            string userName = "name";
            CreateExistingGame();
            JoinGame(uid, userName, ExistingGameName);
            MessagePackets messages = gm.HandleRestart(new RestartMessage(userName, true));

            CollectionAssert.DoesNotContain(gm.GetPlayers().Select(p => p.Uid).ToList(), uid);
            Assert.IsFalse(gm.HasEnded());

            Assert.IsTrue(Utils.VerifyMessagePackets(messages, typeof(RestartMessage), typeof(DisconnectMessage)));

            RestartMessage rm = (RestartMessage)messages.Messages[0].Message;
            Assert.AreEqual(messages.Messages[0].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[0].SendTo.Single(), uid);
            Assert.AreEqual(rm.PlayerName, userName);
            Assert.IsTrue(rm.NewGame);

            DisconnectMessage d = (DisconnectMessage)messages.Messages[1].Message;
            Assert.AreEqual(messages.Messages[1].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[1].SendTo.Single(), ExistingUid);
            Assert.AreEqual(d.PlayerName, userName);
            Assert.IsFalse(d.ShouldDisableGame);
        }

        [TestMethod]
        public void TestHandleRestartGameStarted()
        {
            // TODO
        }

        [TestMethod]
        public void TestHandleRestartSameGame()
        {
            string uid = "uid";
            string userName = "name";
            CreateExistingGame();
            JoinGame(uid, userName, ExistingGameName);
            JoinGame("uid2", "2", ExistingGameName);
            JoinGame("uid3", "3", ExistingGameName);
            MessagePackets messages = gm.HandleRestart(new RestartMessage(userName, true));

            CollectionAssert.DoesNotContain(gm.GetPlayers().Select(p => p.Uid).ToList(), uid);
            Assert.IsFalse(gm.HasEnded());
            Assert.IsTrue(gm.HasStarted());

            Assert.IsTrue(Utils.VerifyMessagePackets(messages, typeof(RestartMessage), typeof(DisconnectMessage)));

            RestartMessage rm = (RestartMessage)messages.Messages[0].Message;
            Assert.AreEqual(messages.Messages[0].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[0].SendTo.Single(), uid);
            Assert.AreEqual(rm.PlayerName, userName);
            Assert.IsTrue(rm.NewGame);

            DisconnectMessage d = (DisconnectMessage)messages.Messages[1].Message;
            Assert.AreEqual(messages.Messages[1].SendTo.Count(), 1);
            Assert.AreEqual(messages.Messages[1].SendTo.Single(), ExistingUid);
            Assert.AreEqual(d.PlayerName, userName);
            Assert.IsFalse(d.ShouldDisableGame);
        }

        [TestMethod]
        public void TestHandleRestartOutOfOrder()
        {
            Assert.ThrowsException<Exception>(() => gm.HandleRestart(new RestartMessage("name", false)));
            // TODO: same game can't happen when game isn't started
        }

        [TestMethod]
        public void TestHandleTurn()
        {
            // TODO
        }

        [TestMethod]
        public void TestHandleTurnOutOfOrder()
        {
            // TODO: can't do until game is started, if game is over, and can't do out of turn
            Assert.ThrowsException<Exception>(() => gm.HandleTurn(new TurnMessage("name")));
        }

        [TestMethod]
        public void TestHandleBid()
        {
            Assert.ThrowsException<Exception>(() => gm.HandleBid(new BidMessage("name")));
        }

        [TestMethod]
        public void TestHandleKitty()
        {
            Assert.ThrowsException<Exception>(() => gm.HandleKitty(new KittyMessage(new Card[0], "name")));
        }

        [TestMethod]
        public void TestHandleTrump()
        {
            Assert.ThrowsException<Exception>(() => gm.HandleTrump(new TrumpMessage("name")));
        }

        [TestMethod]
        public void TestHandleMeld()
        {
            Assert.ThrowsException<Exception>(() => gm.HandleMeld(new MeldMessage("name", "trump", 0)));
        }

        [TestMethod]
        public void TestHandlePass()
        {
            Assert.ThrowsException<Exception>(() => gm.HandlePass(new PassMessage("name")));
        }

        private void CreateExistingGame()
        {
            JoinGame(ExistingUid, ExistingUserName, ExistingGameName);
        }

        private MessagePackets JoinGame(string uid, string userName, string gameName)
        {
            return gm.HandleJoin(uid, new JoinMessage(userName, gameName));
        }
    }
}
