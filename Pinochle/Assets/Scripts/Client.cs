using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using WebSocketSharp;

public class Client : MonoBehaviour
{
    public JoinView JoinPopUp;
    public GameLogView GameLog;
    public GameView GameTable;
    public BidView BidPopUp;
    public MeldView MeldSheet;
    public TrumpView TrumpPopUp;
    public GameObject GameOverScreen;
    public TextMeshProUGUI WinningPlayerText;

    public static Client Instance;

    private WebSocket Ws;
    private List<Response> ResponseQueue;
    private Dictionary<int, string> PlayerOrderMap;
    private string PlayerName;
    private string WinningPlayer;
    private int NumPlayers;
    private bool BidEnabled;
    private bool MeldEnabled;
    private bool TrumpEnabled;
    private bool GameOverEnabled;
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
        BidEnabled = false;
        MeldEnabled = false;
        TrumpEnabled = false;
        GameOverEnabled = false;
        StartInitialized = false;

        Ws = new WebSocket("ws://18.217.141.221:2000");
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

    void Update()
    {
        if (BidEnabled != BidPopUp.gameObject.activeInHierarchy)
        {
            BidPopUp.gameObject.SetActive(BidEnabled);
        }
        if (MeldEnabled != MeldSheet.gameObject.activeInHierarchy)
        {
            MeldSheet.gameObject.SetActive(MeldEnabled);
        }
        if (TrumpEnabled != TrumpPopUp.gameObject.activeInHierarchy)
        {
            TrumpPopUp.gameObject.SetActive(TrumpEnabled);
        }
        if (GameOverEnabled != GameOverScreen.activeInHierarchy)
        {
            WinningPlayerText.text = WinningPlayer;
            GameOverScreen.SetActive(GameOverEnabled);
        }
    }

    void OnApplicationQuit()
    {
        Ws.Close();
    }

    public void DebugLog(string debug)
    {
        GameLog.UpdateLog("DEBUG: ", debug);
        Debug.Log("DEBUG: " + debug);
    }

    public Response HandleJoinGame(string userName, string gameName=null)
    {
        Response response = MessageAndWait(new JoinMessage(userName, gameName));
        if (response.Success)
        {
            PlayerName = userName;
            GameTable.gameObject.SetActive(true);
            GameLog.UpdateLog(SystemString, "The game will start once enough players have joined.");
        }
        return response;
    }

    public void HandleBid(int curBid, int bid)
    {
        BidEnabled = false;
        MessageServer(new BidMessage(PlayerName, curBid, bid));
    }

    public void HandleDiscardKitty(IEnumerable<Card> discards)
    {
        MessageServer(new KittyMessage(discards.ToArray(), PlayerName));
    }

    public void HandleTrump(string trump)
    {
        TrumpEnabled = false;
        MessageServer(new TrumpMessage(PlayerName, trump));
    }

    public void HandleMeld(MeldMessage message)
    {
        MeldEnabled = false;
        MessageServer(message);
    }

    public void HandleTurn(Card card)
    {
        MessageServer(new TurnMessage(PlayerName, card: card));
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

    private void HandleStart(StartMessage message)
    {
        // Wait until previous round is cleared
        GameTable.WaitForCardsCleared();

        if (!StartInitialized)
        {
            // Wait for the view to initalize
            GameTable.WaitForInitialization();

            // Wait for all join messages to be received
            WaitForAllPlayers();

            // Fill in names on table and log
            int playerOrder = PlayerOrderMap.Where(kvp => kvp.Value == PlayerName).Select(kvp => kvp.Key).Single();
            foreach (int i in PlayerOrderMap.Keys)
            {
                if (i != playerOrder)
                {
                    GameTable.ShowPlayerName(playerOrder - i == 1 || playerOrder - i == -1 * (PlayerOrderMap.Count - 1), PlayerOrderMap[i]);
                }
                else
                {
                    GameTable.ShowPlayerName(PlayerOrderMap[i]);
                }
            }
            GameLog.SetNames(PlayerOrderMap);

            StartInitialized = true;
        }

        // Display cards and meld sheet
        GameTable.ShowCards(message.Cards.ToList());
        MeldEnabled = true;
    }

    private void HandleMessage(string message)
    {
        try
        {
            Response response = JsonConvert.DeserializeObject<Response>(message);
            if (response.IsValid())
            {
                Monitor.Enter(ResponseQueue);
                ResponseQueue.Add(response);
                Monitor.PulseAll(ResponseQueue);
                Monitor.Exit(ResponseQueue);
                return;
            }

            AvailableGamesMessage gamesMessage = JsonConvert.DeserializeObject<AvailableGamesMessage>(message);
            if (gamesMessage.IsValid())
            {
                JoinPopUp.HandleAvailableGames(gamesMessage.AvailableGames);
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
                Monitor.Enter(PlayerOrderMap);
                PlayerOrderMap.Add(joinMessage.Order, joinMessage.UserName);
                Monitor.PulseAll(PlayerOrderMap);
                Monitor.Exit(PlayerOrderMap);
                GameLog.UpdateLog(joinMessage.UserName, joinMessage.ToString());
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
                // Wait until previous round is cleared
                GameTable.WaitForCardsCleared();

                if (bidMessage.Bid < 0)
                {
                    BidPopUp.Init(bidMessage.CurBid);
                    BidEnabled = true;
                }
                else if (bidMessage.Bid == bidMessage.CurBid)
                {
                    GameLog.SetBidder(bidMessage.PlayerName);
                    GameLog.SetBid(bidMessage.Bid.ToString());
                }
                else
                {
                    GameLog.UpdateLog(bidMessage.PlayerName, bidMessage.ToString());
                }
                return;
            }

            KittyMessage kittyMessage = JsonConvert.DeserializeObject<KittyMessage>(message);
            if (kittyMessage.IsValid())
            {
                GameTable.ShowKittyCards(kittyMessage.Kitty);
                if (kittyMessage.ChoosingPlayer == PlayerName)
                {
                    GameTable.HandleDiscardKitty(kittyMessage.Kitty.Length);
                }
                else
                {
                    GameLog.UpdateLog(kittyMessage.ChoosingPlayer, kittyMessage.ToString());
                    GameTable.EnableClearKitty();
                }
                return;
            }

            TrumpMessage trumpMessage = JsonConvert.DeserializeObject<TrumpMessage>(message);
            if (trumpMessage.IsValid())
            {
                if (trumpMessage.ChoosingPlayer == PlayerName && trumpMessage.TrumpSuit == string.Empty)
                {
                    TrumpEnabled = true;
                }
                else
                {
                    GameLog.SetTrump(trumpMessage.TrumpSuit);
                    GameLog.UpdateLog(trumpMessage.ChoosingPlayer, trumpMessage.ToString());
                }
                return;
            }

            MeldPointsMessage meldPointsMessage = JsonConvert.DeserializeObject<MeldPointsMessage>(message);
            if (meldPointsMessage.IsValid())
            {
                MeldSheet.SetMeldPoints(meldPointsMessage);
                return;
            }

            MeldMessage meldMessage = JsonConvert.DeserializeObject<MeldMessage>(message);
            if (meldMessage.IsValid())
            {
                if (meldMessage.PlayerName == PlayerName)
                {
                    MeldSheet.SetMeld(meldMessage);
                }
                else
                {
                    // TODO: DISPLAY OTHER PLAYER MELD BETTER
                    GameLog.UpdateLog(meldMessage.PlayerName, meldMessage.ToString());
                }
                return;
            }

            ScoreMessage scoreMessage = JsonConvert.DeserializeObject<ScoreMessage>(message);
            if (scoreMessage.IsValid())
            {
                GameLog.UpdateScore(scoreMessage.PlayerName, scoreMessage.Score);
                return;
            }

            TurnMessage turnMessage = JsonConvert.DeserializeObject<TurnMessage>(message);
            if (turnMessage.IsValid())
            {
                // Wait until previous round is cleared
                GameTable.WaitForCardsCleared();

                if (turnMessage.Card == null)
                {
                    GameTable.EnableTurn(turnMessage.ValidCards);
                }
                else
                {
                    GameTable.ShowPlayedCard(turnMessage.Card, turnMessage.PlayerName, turnMessage.PlayerName == PlayerName);
                }
                return;
            }

            TrickMessage trickMessage = JsonConvert.DeserializeObject<TrickMessage>(message);
            if (trickMessage.IsValid())
            {
                GameTable.EnableClearTrick();
                return;
            }

            GameOverMessage gameOverMessage = JsonConvert.DeserializeObject<GameOverMessage>(message);
            if (gameOverMessage.IsValid())
            {
                WinningPlayer = gameOverMessage.WinningPlayer;
                GameOverEnabled = true;
                return;
            }
        }
        catch (Exception err)
        {
            Debug.Log("OnMessage error: " + err.Message);
            Debug.Log("OnMessage stack trace: " + err.StackTrace);
            DebugLog("OnMessage error: " + err.Message);
            DebugLog("OnMessage stack trace: " + err.StackTrace);
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

    private void MessageServer(Message message)
    {
        if (Ws != null && Ws.IsAlive)
        {
            Ws.Send(JsonConvert.SerializeObject(message));
        }
    }
}
