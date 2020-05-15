using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;

public class Client : MonoBehaviour
{
    public static Client Instance;

    private WebSocket Ws;
    private Queue<Task> MessageTasks;
    private Dictionary<int, string> PlayerOrderMap;
    private string PlayerName;
    private bool StartInitialized;

    private static readonly string SystemString = "System";
    private static readonly Dictionary<string, string> SuitUnicodeMap = new Dictionary<string, string>()
    {
        { "C", "<color=#" + GameView.BlackText + ">\u2663</color>" },
        { "D", "<color=#" + GameView.RedText + ">\u2666</color>" },
        { "S", "<color=#" + GameView.BlackText + ">\u2660</color>" },
        { "H", "<color=#" + GameView.RedText + ">\u2665</color>" }
    };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        MessageTasks = new Queue<Task>();
        PlayerOrderMap = new Dictionary<int, string>();
        StartInitialized = false;

        Ws = new WebSocket("ws://18.217.141.221:2000");

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
            lock (MessageTasks)
            {
                MessageTasks.Enqueue(new Task(() => HandleMessage(message)));
            }
        };

        Ws.Connect();
    }

    void Update()
    {
        lock (MessageTasks)
        {
            while (MessageTasks.Count > 0)
            {
                MessageTasks.Dequeue().RunSynchronously();
            }
        }
    }

    void OnApplicationQuit()
    {
        Ws.Close();
    }

    public void SubmitGameType(string gameType)
    {
        MessageServer(new GameTypeMessage(game: gameType));
    }

    public void SubmitJoinGame(string userName, string gameName=null)
    {
        MessageServer(new JoinMessage(userName, gameName));
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
        ViewController.Instance.UpdateLog(message.PlayerName, message.ToString());
        ViewController.Instance.ShowMeldWindow(false);
        MessageServer(message);
    }

    public void SubmitPass(Card[] passedCards)
    {
        MessageServer(new PassMessage(PlayerName, cards: passedCards));
    }

    public void SubmitTurn(Card card)
    {
        MessageServer(new TurnMessage(PlayerName, card: card));
    }

    private void HandleMessage(string message)
    {
        try
        {
            lock (MessageTasks)
            {
                JoinResponse joinResponse = JsonConvert.DeserializeObject<JoinResponse>(message);
                if (joinResponse.IsValid())
                {
                    HandleJoinResponse(joinResponse);
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

                PassMessage passMessage = JsonConvert.DeserializeObject<PassMessage>(message);
                if (passMessage.IsValid())
                {
                    HandlePass(passMessage);
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
        }
        catch (Exception err)
        {
            Debug.Log("OnMessage error: " + err.Message);
            Debug.Log("OnMessage stack trace: " + err.StackTrace);
        }
    }

    private void HandleJoinResponse(JoinResponse joinResponse)
    {
        if (joinResponse.Success)
        {
            PlayerName = joinResponse.UserName;
            ViewController.Instance.ShowGameTable(true);
            ViewController.Instance.UpdateLog(SystemString, "The game will start once enough players have joined.");
            ViewController.Instance.HideJoinWindow();
        }
        else
        {
            ViewController.Instance.SetJoinError(joinResponse.ErrorMessage);
        }
    }

    private void HandleJoin(JoinMessage joinMessage)
    {
        PlayerOrderMap.Add(joinMessage.Order, joinMessage.UserName);
        ViewController.Instance.UpdateLog(joinMessage.UserName, joinMessage.ToString());
    }

    private void HandleStart(StartMessage message)
    {
        if (!StartInitialized)
        {
            ViewController.Instance.UpdateNames(PlayerOrderMap, PlayerName);
            ViewController.Instance.ShowCardsInHand(message.Cards.ToList());
            StartInitialized = true;
        }
        else
        {
            ViewController.Instance.DoOnClear(() =>
            {
                ViewController.Instance.UnHighlightNames();
                ViewController.Instance.ClearInfo();
                ViewController.Instance.ShowCardsInHand(message.Cards.ToList());
            });
        }
    }

    private void HandleBid(BidMessage bidMessage)
    {
        ViewController.Instance.DoOnClear(() =>
        {
            if (bidMessage.Bid < 0)
            {
                if (bidMessage.PlayerName == PlayerName)
                {
                    ViewController.Instance.ShowBidWindow(true, bidMessage.CurBid);
                }
                else
                {
                    ViewController.Instance.UpdateLog(bidMessage.PlayerName, "Bidding...");
                }
            }
            else if (bidMessage.Bid == bidMessage.CurBid)
            {
                ViewController.Instance.UpdateBidInfo(bidMessage.PlayerName, bidMessage.Bid.ToString());
                ViewController.Instance.UpdateLog(SystemString, "The final bid is " + bidMessage.Bid.ToString() + " by " + bidMessage.PlayerName);
            }
            else
            {
                ViewController.Instance.UpdateLog(bidMessage.PlayerName, bidMessage.ToString());
            }
        });
    }

    private void HandleKitty(KittyMessage kittyMessage)
    {
        ViewController.Instance.ShowCardsInKitty(kittyMessage.Kitty);
        ViewController.Instance.UpdateLog(kittyMessage.ChoosingPlayer, kittyMessage.ToString());
        if (kittyMessage.ChoosingPlayer == PlayerName)
        {
            ViewController.Instance.EnableDiscardCardsInKitty(kittyMessage.Kitty.Length);
        }
        else
        {
            ViewController.Instance.EnableClearCardsInKitty();
        }
    }

    private void HandleTrump(TrumpMessage trumpMessage)
    {
        if (trumpMessage.TrumpSuit == string.Empty)
        {
            ViewController.Instance.UpdateLog(trumpMessage.ChoosingPlayer, "Choosing trump...");
            if (trumpMessage.ChoosingPlayer == PlayerName)
            {
                ViewController.Instance.ShowTrumpWindow(true);
            }
        }
        else
        {
            ViewController.Instance.UpdateTrumpInfo(SuitUnicodeMap[trumpMessage.TrumpSuit]);
            ViewController.Instance.UpdateLog(trumpMessage.ChoosingPlayer, trumpMessage.ToString() + SuitUnicodeMap[trumpMessage.TrumpSuit]);
        }
    }

    private void HandleMeldPoints(MeldPointsMessage meldPointsMessage)
    {
        ViewController.Instance.DoOnClear(() =>
        {
            ViewController.Instance.ShowMeldWindow(true, meldPointsMessage);
        });
    }

    private void HandleMeld(MeldMessage meldMessage)
    {
        if (meldMessage.PlayerName == PlayerName)
        {
            ViewController.Instance.EnableSubmitMeld(meldMessage);
        }
        else
        {
            ViewController.Instance.UpdateLog(meldMessage.PlayerName, meldMessage.ToString());
        }
    }

    private void HandlePass(PassMessage passMessage)
    {
        ViewController.Instance.DoOnClear(() =>
        {
            if (passMessage.PassingPlayer == passMessage.PassingTo)
            {
                // no pass
                ViewController.Instance.UpdatePassingInfo("No pass");
                SubmitPass(new Card[0]);
            }
            else
            {
                if (passMessage.PassedCards == null)
                {
                    ViewController.Instance.UpdatePassingInfo(passMessage.PassingTo);
                    ViewController.Instance.EnablePass(passMessage.NumToPass);
                }
                else
                {
                    ViewController.Instance.AddCardsInHand(passMessage.PassedCards.ToList());
                }
            }
        });
    }

    private void HandleTurn(TurnMessage turnMessage)
    {
        ViewController.Instance.DoOnClear(() =>
        {
            if (turnMessage.IsFirstCard)
            {
                ViewController.Instance.UnHighlightNames();
                ViewController.Instance.HighlightName(turnMessage.PlayerName);
            }

            if (turnMessage.Card == null)
            {
                if (turnMessage.PlayerName == PlayerName)
                {
                    ViewController.Instance.EnablePlayCard(turnMessage.ValidCards);
                }
            }
            else
            {
                ViewController.Instance.ShowPlayedCard(turnMessage.Card, turnMessage.PlayerName, turnMessage.PlayerName == PlayerName);
            }
        });
    }

    private void MessageServer(Message message)
    {
        if (Ws != null && Ws.IsAlive)
        {
            Ws.Send(JsonConvert.SerializeObject(message));
        }
    }
}
