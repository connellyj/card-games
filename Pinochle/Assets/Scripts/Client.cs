using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using WebSocketSharp;

public class Client : MonoBehaviour
{
    public static Client Instance;

    private WebSocket Ws;
    private List<Response> ResponseQueue;
    private Dictionary<int, string> PlayerOrderMap;
    private string PlayerName;
    private int NumPlayers;
    private bool StartInitialized;

    private static readonly string SystemString = "System";

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ResponseQueue = new List<Response>();
        PlayerOrderMap = new Dictionary<int, string>();
        StartInitialized = false;

        Ws = new WebSocket("ws://localhost:2000");
        Ws.Connect();

        Ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Connected to websocket");
        };

        Ws.OnClose += (sender, e) =>
        {
            Debug.Log("Connection closed");
        };

        Ws.OnError += (sender, e) =>
        {
            Debug.Log("WebSocket error: " + e.Message);
        };

        Ws.OnMessage += (sender, e) =>
        {
            string message = e.Data;
            Debug.Log("Server: " + message);
            HandleMessage(message);
        };
    }

    void OnApplicationQuit()
    {
        Ws.Close();
    }

    public void SubmitGameType(string gameType)
    {
        MessageServer(new GameTypeMessage(game: gameType));
    }

    public Response SubmitJoinGame(string userName, string gameName=null)
    {
        Response response = MessageAndWait(new JoinMessage(userName, gameName));
        if (response.Success)
        {
            PlayerName = userName;
            ViewController.Instance.ShowGameTable(true);
            ViewController.Instance.UpdateLog(SystemString, "The game will start once enough players have joined.");
        }
        return response;
    }

    public void SubmitBid(int curBid, int bid)
    {
        ViewController.Instance.ShowBidWindow(false);
        MessageServer(new BidMessage(PlayerName, curBid, bid));
    }

    public void SubmitDiscardKitty(IEnumerable<Card> discards)
    {
        MessageServer(new KittyMessage(discards.ToArray(), PlayerName));
    }

    public void SubmitTrump(string trump)
    {
        ViewController.Instance.ShowTrumpWindow(false);
        MessageServer(new TrumpMessage(PlayerName, trump));
    }

    public void SubmitMeld(MeldMessage message)
    {
        ViewController.Instance.ShowMeldWindow(false);
        MessageServer(message);
    }

    public void SubmitTurn(Card card)
    {
        MessageServer(new TurnMessage(PlayerName, card: card));
    }

    private void HandleMessage(string message)
    {
        try
        {
            Response response = JsonConvert.DeserializeObject<Response>(message);
            if (response.IsValid())
            {
                HandleResponse(response);
                return;
            }

            GameTypeMessage gameTypeMessage = JsonConvert.DeserializeObject<GameTypeMessage>(message);
            if (gameTypeMessage.IsValid())
            {
                ViewController.Instance.UpdateGameTypes(gameTypeMessage.GameTypes);
                return;
            }

                AvailableGamesMessage gamesMessage = JsonConvert.DeserializeObject<AvailableGamesMessage>(message);
            if (gamesMessage.IsValid())
            {
                ViewController.Instance.UpdateAvailableGames(gamesMessage.AvailableGames);
                return;
            }

            GameInfoMessage infoMessage = JsonConvert.DeserializeObject<GameInfoMessage>(message);
            if (infoMessage.IsValid())
            {
                NumPlayers = infoMessage.NumPlayers;
                return;
            }

            JoinMessage joinMessage = JsonConvert.DeserializeObject<JoinMessage>(message);
            if (joinMessage.IsValid())
            {
                HandleJoin(joinMessage);
                return;
            }

            StartMessage startMessage = JsonConvert.DeserializeObject<StartMessage>(message);
            if (startMessage.IsValid())
            {
                HandleStart(startMessage);
                return;
            }

            BidMessage bidMessage = JsonConvert.DeserializeObject<BidMessage>(message);
            if (bidMessage.IsValid())
            {
                HandleBid(bidMessage);
                return;
            }

            KittyMessage kittyMessage = JsonConvert.DeserializeObject<KittyMessage>(message);
            if (kittyMessage.IsValid())
            {
                HandleKitty(kittyMessage);
                return;
            }

            TrumpMessage trumpMessage = JsonConvert.DeserializeObject<TrumpMessage>(message);
            if (trumpMessage.IsValid())
            {
                HandleTrump(trumpMessage);
                return;
            }

            MeldPointsMessage meldPointsMessage = JsonConvert.DeserializeObject<MeldPointsMessage>(message);
            if (meldPointsMessage.IsValid())
            {
                HandleMeldPoints(meldPointsMessage);
                return;
            }

            MeldMessage meldMessage = JsonConvert.DeserializeObject<MeldMessage>(message);
            if (meldMessage.IsValid())
            {
                HandleMeld(meldMessage);
                return;
            }

            ScoreMessage scoreMessage = JsonConvert.DeserializeObject<ScoreMessage>(message);
            if (scoreMessage.IsValid())
            {
                ViewController.Instance.UpdateScoreInfo(scoreMessage.PlayerName, scoreMessage.Score);
                return;
            }

            TurnMessage turnMessage = JsonConvert.DeserializeObject<TurnMessage>(message);
            if (turnMessage.IsValid())
            {
                HandleTurn(turnMessage);
                return;
            }

            TrickMessage trickMessage = JsonConvert.DeserializeObject<TrickMessage>(message);
            if (trickMessage.IsValid())
            {
                ViewController.Instance.EnableClearPlayedCards();
                return;
            }

            GameOverMessage gameOverMessage = JsonConvert.DeserializeObject<GameOverMessage>(message);
            if (gameOverMessage.IsValid())
            {
                ViewController.Instance.ShowGameOverWindow(true, gameOverMessage.WinningPlayer);
                return;
            }
        }
        catch (Exception err)
        {
            Debug.Log("OnMessage error: " + err.Message);
            Debug.Log("OnMessage stack trace: " + err.StackTrace);
        }
    }

    private void HandleResponse(Response response)
    {
        Monitor.Enter(ResponseQueue);
        ResponseQueue.Add(response);
        Monitor.PulseAll(ResponseQueue);
        Monitor.Exit(ResponseQueue);
    }

    private void HandleJoin(JoinMessage joinMessage)
    {
        Monitor.Enter(PlayerOrderMap);
        PlayerOrderMap.Add(joinMessage.Order, joinMessage.UserName);
        Monitor.PulseAll(PlayerOrderMap);
        Monitor.Exit(PlayerOrderMap);
        ViewController.Instance.UpdateLog(joinMessage.UserName, joinMessage.ToString());
    }

    private void HandleStart(StartMessage message)
    {
        // Wait until previous round is cleared
        ViewController.Instance.WaitForCardsCleared();

        if (!StartInitialized)
        {
            WaitForAllPlayers();
            ViewController.Instance.UpdateNames(PlayerOrderMap, PlayerName);
            StartInitialized = true;
        }

        // Display cards and meld sheet
        ViewController.Instance.ShowCardsInHand(message.Cards.ToList());
    }

    private void HandleBid(BidMessage bidMessage)
    {
        ViewController.Instance.WaitForCardsCleared();
        if (bidMessage.Bid < 0)
        {
            ViewController.Instance.ShowBidWindow(true, bidMessage.CurBid);
        }
        else if (bidMessage.Bid == bidMessage.CurBid)
        {

            ViewController.Instance.UpdateBidInfo(bidMessage.PlayerName, bidMessage.Bid.ToString());
        }
        else
        {
            ViewController.Instance.UpdateLog(bidMessage.PlayerName, bidMessage.ToString());
        }
    }

    private void HandleKitty(KittyMessage kittyMessage)
    {
        ViewController.Instance.ShowCardsInKitty(kittyMessage.Kitty);
        if (kittyMessage.ChoosingPlayer == PlayerName)
        {
            ViewController.Instance.EnableDiscardCardsInKitty(kittyMessage.Kitty.Length);
        }
        else
        {
            ViewController.Instance.UpdateLog(kittyMessage.ChoosingPlayer, kittyMessage.ToString());
            ViewController.Instance.EnableClearCardsInKitty();
        }
    }

    private void HandleTrump(TrumpMessage trumpMessage)
    {
        if (trumpMessage.ChoosingPlayer == PlayerName && trumpMessage.TrumpSuit == string.Empty)
        {
            ViewController.Instance.ShowTrumpWindow(true);
        }
        else
        {
            ViewController.Instance.UpdateTrumpInfo(trumpMessage.TrumpSuit);
            ViewController.Instance.GameLog.UpdateLog(trumpMessage.ChoosingPlayer, trumpMessage.ToString());
        }
    }

    private void HandleMeldPoints(MeldPointsMessage meldPointsMessage)
    {
        ViewController.Instance.ShowMeldWindow(true, meldPointsMessage);
    }

    private void HandleMeld(MeldMessage meldMessage)
    {
        if (meldMessage.PlayerName == PlayerName)
        {
            ViewController.Instance.EnableSubmitMeld(meldMessage);
        }
        else
        {
            ViewController.Instance.GameLog.UpdateLog(meldMessage.PlayerName, meldMessage.ToString());
        }
    }

    private void HandleTurn(TurnMessage turnMessage)
    {
        ViewController.Instance.WaitForCardsCleared();
        if (turnMessage.Card == null)
        {
            ViewController.Instance.EnablePlayCard(turnMessage.ValidCards);
        }
        else
        {
            ViewController.Instance.ShowPlayedCard(turnMessage.Card, turnMessage.PlayerName, turnMessage.PlayerName == PlayerName);
        }
    }

    private void WaitForAllPlayers()
    {
        Monitor.Enter(PlayerOrderMap);
        while (PlayerOrderMap.Count < NumPlayers)
        {
            Monitor.Wait(PlayerOrderMap);
        }
        Monitor.Exit(PlayerOrderMap);
    }

    private Response MessageAndWait(Message message)
    {
        MessageServer(message);
        return WaitForResponse(message.GenerateId());
    }

    private Response WaitForResponse(string hashCode)
    {
        if (Ws.IsAlive)
        {
            Monitor.Enter(ResponseQueue);
            while (!ResponseQueue.Select(r => r.MessageId).Contains(hashCode))
            {
                Monitor.Wait(ResponseQueue);
            }
            Response response = ResponseQueue.Where(r => r.MessageId == hashCode).Single();
            ResponseQueue.Remove(response);
            Monitor.Exit(ResponseQueue);
            return response;
        }
        else
        {
            return new Response(false, "abcdefg", "The server isn't running");
        }
    }

    private void MessageServer(Message message)
    {
        if (Ws != null && Ws.IsAlive)
        {
            Ws.Send(JsonConvert.SerializeObject(message));
        }
    }
}
