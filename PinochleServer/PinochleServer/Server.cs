using Newtonsoft.Json;
using CardGameServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace CardGameServer
{
    class Server
    {
        private static Server instance;

        private readonly WebSocketServer SocketServer;

        private Server()
        {
            SocketServer = new WebSocketServer(2000);
            SocketServer.AddWebSocketService<Router>("/");
            GameManager.Init();
        }

        ~Server()
        {
            SocketServer.Stop();
        }

        public static Server Instance()
        {
            if (instance == null)
            {
                instance = new Server();
            }
            return instance;
        }

        public void Start()
        {
            SocketServer.Start();
            Console.ReadKey(true);  // keeps program active forever
        }

        public void Broadcast<Model>(Model message)
        {
            SocketServer.WebSocketServices["/"].Sessions.Broadcast(JsonConvert.SerializeObject(message));
        }

        public void Broadcast<Model>(Model message, IEnumerable<string> uids)
        {
            foreach (string uid in SocketServer.WebSocketServices["/"].Sessions.ActiveIDs.Intersect(uids))
            {
                SocketServer.WebSocketServices["/"].Sessions.SendTo(JsonConvert.SerializeObject(message), uid);
            }
        }

        public void Send<Model>(Model message, string uid)
        {
            SocketServer.WebSocketServices["/"].Sessions.SendTo(JsonConvert.SerializeObject(message), uid);
        }

        private class Router : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                string message = e.Data;
                Console.WriteLine(message);
                HandleMessage(message);
            }

            protected override void OnOpen()
            {
                foreach (Message m in GameManager.HandleNewConnection())
                {
                    Send(JsonConvert.SerializeObject(m));
                }
            }

            private void HandleMessage(string message)
            {
                JoinMessage joinMessage = JsonConvert.DeserializeObject<JoinMessage>(message);
                if (joinMessage.IsValid())
                {
                    GameManager.HandleJoin(joinMessage, ID, joinMessage.GenerateId());
                    return;
                }

                BidMessage bidMessage = JsonConvert.DeserializeObject<BidMessage>(message);
                if (bidMessage.IsValid())
                {
                    GameManager.HandleBid(ID, bidMessage);
                    return;
                }

                KittyMessage kittyMessage = JsonConvert.DeserializeObject<KittyMessage>(message);
                if (kittyMessage.IsValid())
                {
                    GameManager.HandleKitty(ID, kittyMessage);
                    return;
                }

                TrumpMessage trumpMessage = JsonConvert.DeserializeObject<TrumpMessage>(message);
                if (trumpMessage.IsValid())
                {
                    GameManager.HandleTrump(ID, trumpMessage);
                    return;
                }

                MeldMessage meldMessage = JsonConvert.DeserializeObject<MeldMessage>(message);
                if (meldMessage.IsValid())
                {
                    GameManager.HandleMeld(ID, meldMessage);
                    return;
                }

                TurnMessage turnMessage = JsonConvert.DeserializeObject<TurnMessage>(message);
                if (turnMessage.IsValid())
                {
                    GameManager.HandleTurn(ID, turnMessage);
                    return;
                }
            }
        }
    }
}
