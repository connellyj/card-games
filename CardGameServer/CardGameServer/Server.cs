using CardGameServer.GameManagers;
using CardGameServer.Models;
using Newtonsoft.Json;
using System;
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

        public void Send(MessagePackets messages)
        {
            foreach (MessagePacket m in messages.Messages)
            {
                LogSent(m.Message);
                if (m.SendTo == null)
                {
                    SocketServer.WebSocketServices["/"].Sessions.Broadcast(JsonConvert.SerializeObject(m.Message));
                }
                else
                {
                    foreach (string uid in m.SendTo)
                    {
                        if (SocketServer.WebSocketServices["/"].Sessions.ActiveIDs.Any(s => s == uid))
                        {
                            SocketServer.WebSocketServices["/"].Sessions.SendTo(JsonConvert.SerializeObject(m.Message), uid);
                        }
                        else
                        {
                            Console.WriteLine("Unable to send to " + uid);
                        }
                    }
                }
            }
        }

        private void LogSent(Message message)
        {
            Console.WriteLine("    Sent: " + JsonConvert.SerializeObject(message));
        }

        private class Router : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                string message = e.Data;
                Console.WriteLine("Received: " + message);
                SendDo(() => HandleMessage(message));
            }

            protected override void OnOpen()
            {
                SendDo(() => GamesManager.Get().HandleNewConnection(ID));
            }

            protected override void OnClose(CloseEventArgs e)
            {
                SendDo(() => GamesManager.Get().HandlePlayerDisconnect(ID));
            }

            protected override void OnError(ErrorEventArgs e)
            {
                Console.WriteLine("WebSocket error: " + e.Message);
            }

            private void SendDo(Func<MessagePackets> messageGenerator)
            {
                try
                {
                    Instance().Send(messageGenerator());
                }
                catch (Exception err)
                {
                    Instance().Send(new MessagePackets(new ErrorResponse(err.Message), ID));
                }
            }

            private MessagePackets HandleMessage(string message)
            {
                RestartMessage restartMessage = JsonConvert.DeserializeObject<RestartMessage>(message);
                if (restartMessage.IsValid())
                {
                    return GamesManager.Get().HandleRestart(ID, restartMessage);
                }

                GameTypeMessage gameTypeMessage = JsonConvert.DeserializeObject<GameTypeMessage>(message);
                if (gameTypeMessage.IsValid())
                {
                    return GamesManager.Get().HandleGameTypes(ID, gameTypeMessage);
                }

                JoinMessage joinMessage = JsonConvert.DeserializeObject<JoinMessage>(message);
                if (joinMessage.IsValid())
                {
                    return GamesManager.Get().HandleJoin(ID, joinMessage);
                }

                BidMessage bidMessage = JsonConvert.DeserializeObject<BidMessage>(message);
                if (bidMessage.IsValid())
                {
                    return GamesManager.Get().HandleBid(ID, bidMessage);
                }

                KittyMessage kittyMessage = JsonConvert.DeserializeObject<KittyMessage>(message);
                if (kittyMessage.IsValid())
                {
                    return GamesManager.Get().HandleKitty(ID, kittyMessage);
                }

                TrumpMessage trumpMessage = JsonConvert.DeserializeObject<TrumpMessage>(message);
                if (trumpMessage.IsValid())
                {
                    return GamesManager.Get().HandleTrump(ID, trumpMessage);
                }

                MeldMessage meldMessage = JsonConvert.DeserializeObject<MeldMessage>(message);
                if (meldMessage.IsValid())
                {
                    return GamesManager.Get().HandleMeld(ID, meldMessage);
                }

                PassMessage passMessage = JsonConvert.DeserializeObject<PassMessage>(message);
                if (passMessage.IsValid())
                {
                    return GamesManager.Get().HandlePass(ID, passMessage);
                }

                TurnMessage turnMessage = JsonConvert.DeserializeObject<TurnMessage>(message);
                if (turnMessage.IsValid())
                {
                    return GamesManager.Get().HandleTurn(ID, turnMessage);
                }

                return new MessagePackets();
            }
        }
    }
}
