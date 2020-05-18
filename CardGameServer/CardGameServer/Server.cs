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
            SocketServer = new WebSocketServer(2000)
            {
                KeepClean = false
            };
            SocketServer.AddWebSocketService<Router>("/");
            SocketServer.WebSocketServices["/"].KeepClean = false;
            GameManager.StaticInitialize();
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
            Console.ReadKey(true);  // keeps program active
        }

        public void Broadcast<Model>(Model message)
        {
            LogSent(message);
            SocketServer.WebSocketServices["/"].Sessions.Broadcast(JsonConvert.SerializeObject(message));
        }

        public void Broadcast<Model>(Model message, IEnumerable<string> uids)
        {
            LogSent(message);
            foreach (string uid in SocketServer.WebSocketServices["/"].Sessions.ActiveIDs.Intersect(uids))
            {
                SocketServer.WebSocketServices["/"].Sessions.SendTo(JsonConvert.SerializeObject(message), uid);
            }
        }

        public void Send<Model>(Model message, string uid)
        {
            LogSent(message);
            if (SocketServer.WebSocketServices["/"].Sessions.ActiveIDs.Any(s => s == uid))
            {
                SocketServer.WebSocketServices["/"].Sessions.SendTo(JsonConvert.SerializeObject(message), uid);
            }
        }

        private void LogSent<Model>(Model message)
        {
            Console.WriteLine("    Sent: " + JsonConvert.SerializeObject(message));
        }

        private class Router : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                string message = e.Data;
                Console.WriteLine("Received: " + message);
                HandleMessage(message);
            }

            protected override void OnOpen()
            {
                Send(JsonConvert.SerializeObject(GameManager.GetGameTypes()));
            }

            protected override void OnClose(CloseEventArgs e)
            {
                GameManager.HandlePlayerDisconnect(ID);
            }

            private void HandleMessage(string message)
            {
                RestartMessage restartMessage = JsonConvert.DeserializeObject<RestartMessage>(message);
                if (restartMessage.IsValid())
                {
                    GameManager.HandleRestart(ID, restartMessage);
                    return;
                }

                GameTypeMessage gameTypeMessage = JsonConvert.DeserializeObject<GameTypeMessage>(message);
                if (gameTypeMessage.IsValid())
                {
                    GameManager.HandleGameTypes(ID, gameTypeMessage);
                    return;
                }

                JoinMessage joinMessage = JsonConvert.DeserializeObject<JoinMessage>(message);
                if (joinMessage.IsValid())
                {
                    GameManager.HandleJoin(ID, joinMessage);
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

                PassMessage passMessage = JsonConvert.DeserializeObject<PassMessage>(message);
                if (passMessage.IsValid())
                {
                    GameManager.HandlePass(ID, passMessage);
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
