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
        { "C", "<color=#" + GameView.BlackText + "><size=1.5em>\u2663</size></color>" },
        { "D", "<color=#" + GameView.RedText + "><size=1.5em>\u2666</size></color>" },
        { "S", "<color=#" + GameView.BlackText + "><size=1.5em>\u2660</size></color>" },
        { "H", "<color=#" + GameView.RedText + "><size=1.5em>\u2665</size></color>" }
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

        //Ws = new WebSocket("ws://localhost:2000");
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

    public void SubmitSettings(bool reverse)
    {
        ViewController.Instance.UpdateSort(reverse);
    }

    public void SubmitRestart(bool newGame)
    {
        MessageServer(new RestartMessage(PlayerName, newGame));
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
        ViewController.Instance.UpdateLog(message.PlayerName, MeldToString(message));
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
                ErrorResponse errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(message);
                if (errorResponse.IsValid())
                {
                    Debug.Log("ErrorResponse: " + errorResponse.ErrorMessage);
                    return;
                }

                DisconnectMessage disconnectMessage = JsonConvert.DeserializeObject<DisconnectMessage>(message);
                if (disconnectMessage.IsValid())
                {
                    HandleDisconnect(disconnectMessage);
                    return;
                }

                RestartMessage restartMessage = JsonConvert.DeserializeObject<RestartMessage>(message);
                if (restartMessage.IsValid())
                {
                    HandleRestart(restartMessage);
                    return;
                }

                GameTypeMessage gameTypeMessage = JsonConvert.DeserializeObject<GameTypeMessage>(message);
                if (gameTypeMessage.IsValid())
                {
                    ViewController.Instance.UpdateGameTypes(gameTypeMessage.GameTypes);
                    return;
                }

                JoinResponse joinResponse = JsonConvert.DeserializeObject<JoinResponse>(message);
                if (joinResponse.IsValid())
                {
                    HandleJoinResponse(joinResponse);
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
                    HandleScore(scoreMessage);
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
                    HandleTrick(trickMessage);
                    return;
                }

                TrickInfoMessage trickInfoMessage = JsonConvert.DeserializeObject<TrickInfoMessage>(message);
                if (trickInfoMessage.IsValid())
                {
                    HandleTrickInfo(trickInfoMessage);
                    return;
                }

                GameOverMessage gameOverMessage = JsonConvert.DeserializeObject<GameOverMessage>(message);
                if (gameOverMessage.IsValid())
                {
                    HandleGameOver(gameOverMessage);
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

    private void HandleDisconnect(DisconnectMessage disconnectMessage)
    {
        if (!disconnectMessage.ShouldDisableGame)
        {
            ViewController.Instance.UpdateLog(disconnectMessage.PlayerName, "Disconnected");
            ViewController.Instance.DisableRestart();
        }
        else
        {
            ViewController.Instance.ShowStoppedGame(true, disconnectMessage.PlayerName);
        }
    }

    private void HandleRestart(RestartMessage restartMessage)
    {
        ViewController.Instance.ClearGameLog();
        ViewController.Instance.ShowGameOverWindow(false);
        ViewController.Instance.ShowStoppedGame(false);
        if (!restartMessage.NewGame)
        {
            ViewController.Instance.UpdateLog(restartMessage.PlayerName, "Restarted the game");
        }
        else
        {
            ViewController.Instance.ShowGameTypePopUp(true);
            ViewController.Instance.ShowGameTable(false);
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
        ViewController.Instance.UpdateLog(joinMessage.UserName, "Joined game: '" + joinMessage.GameName + "'");
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
                string bidStr = bidMessage.Bid == 0 ? "Pass" : string.Format("Bid {0}", bidMessage.Bid.ToString());
                ViewController.Instance.UpdateLog(bidMessage.PlayerName, bidStr);
            }
        });
    }

    private void HandleKitty(KittyMessage kittyMessage)
    {
        ViewController.Instance.ShowCardsInKitty(kittyMessage.Kitty);
        ViewController.Instance.UpdateLog(kittyMessage.ChoosingPlayer, string.Format("Choosing {0} cards to discard...", kittyMessage.Kitty.Length));
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
                if (trumpMessage.ExtraOptions != null)
                {
                    ViewController.Instance.UpdateExtraTrumpOptions(trumpMessage.ExtraOptions);
                }
                if (trumpMessage.UnavailableOptions != null)
                {
                    ViewController.Instance.UpdateDisabledTrumpOptions(trumpMessage.UnavailableOptions);
                }
                ViewController.Instance.ShowTrumpWindow(true);
            }
        }
        else
        {
            string trumpOption = SuitUnicodeMap.ContainsKey(trumpMessage.TrumpSuit) ? SuitUnicodeMap[trumpMessage.TrumpSuit] : trumpMessage.TrumpSuit;
            ViewController.Instance.UpdateTrumpInfo(trumpOption);
            string trumpStr = trumpMessage.TrumpSuit == string.Empty ? "Choosing trump..." : "Trump is: " + trumpOption;
            ViewController.Instance.UpdateLog(trumpMessage.ChoosingPlayer, trumpStr);
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
            ViewController.Instance.UpdateLog(meldMessage.PlayerName, MeldToString(meldMessage));
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
                    ViewController.Instance.UpdateLog(SystemString, "Everyone is choosing cards to pass...");
                }
                else
                {
                    ViewController.Instance.AddCardsInHand(passMessage.PassedCards.ToList());
                    string cards = string.Join(", ", passMessage.PassedCards.Select(c => c.Rank + SuitUnicodeMap[c.Suit]));
                    ViewController.Instance.UpdateLog(passMessage.PassingPlayer, "Passed: " + cards);
                }
            }
        });
    }

    private void HandleTrick(TrickMessage trickMessage)
    {
        ViewController.Instance.EnableClearPlayedCards();
    }

    private void HandleTrickInfo(TrickInfoMessage trickInfoMessage)
    {
        foreach (string key in trickInfoMessage.TricksLeft.Keys)
        {
            ViewController.Instance.UpdateLog(key, "Needs " + trickInfoMessage.TricksLeft[key].ToString() + " more tricks");
        }
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

    private void HandleScore(ScoreMessage scoreMessage)
    {
        ViewController.Instance.UpdateLog(scoreMessage.PlayerName, "Gained " + scoreMessage.ScoreDif.ToString() + " points");
        if (scoreMessage.MissedBy > 0)
        {
            ViewController.Instance.UpdateLog(scoreMessage.PlayerName, "Missed bid by " + scoreMessage.MissedBy.ToString() + " points");
        }
        ViewController.Instance.UpdateScoreInfo(scoreMessage.PlayerName, scoreMessage.Score);
    }

    private void HandleGameOver(GameOverMessage gameOverMessage)
    {
        ViewController.Instance.DoOnClear(() => ViewController.Instance.ShowGameOverWindow(true, gameOverMessage.WinningPlayer));
    }

    private void MessageServer(Message message)
    {
        if (Ws.IsAlive)
        {
            Ws.Send(JsonConvert.SerializeObject(message));
        }
    }

    private string MeldToString(MeldMessage meldMessage)
    {
        List<string> meld = new List<string>();
        if (meldMessage.AcesAround > 0) meld.Add("Aces Around x" + meldMessage.AcesAround.ToString());
        if (meldMessage.KingsAround > 0) meld.Add("Kings Around x" + meldMessage.KingsAround.ToString());
        if (meldMessage.QueensAround > 0) meld.Add("Queens Around x" + meldMessage.QueensAround.ToString());
        if (meldMessage.JacksAround > 0) meld.Add("Jacks Around x" + meldMessage.JacksAround.ToString());
        if (meldMessage.ClubsMarriage > 0) meld.Add(SuitUnicodeMap["C"] + " Marriage x" + meldMessage.ClubsMarriage.ToString());
        if (meldMessage.DiamondsMarriage > 0) meld.Add(SuitUnicodeMap["D"] + " Marriage x" + meldMessage.DiamondsMarriage.ToString());
        if (meldMessage.SpadesMarriage > 0) meld.Add(SuitUnicodeMap["S"] + " Marriage x" + meldMessage.SpadesMarriage.ToString());
        if (meldMessage.HeartsMarriage > 0) meld.Add(SuitUnicodeMap["H"] + " Marriage x" + meldMessage.HeartsMarriage.ToString());
        if (meldMessage.Pinochle > 0) meld.Add("Pinochle x" + meldMessage.Pinochle.ToString());
        if (meldMessage.Run > 0) meld.Add("Run x" + meldMessage.Run.ToString());
        if (meldMessage.TrumpNine > 0) meld.Add("9 x" + meldMessage.TrumpNine.ToString());
        string meldStr = meld.Count > 0 ? string.Join(", ", meld) : "None :(";
        return "Meld: " + meldStr + " for a total of " + meldMessage.TotalPoints.ToString() + " points";
    }
}
