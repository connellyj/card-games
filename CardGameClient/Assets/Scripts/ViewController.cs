using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ViewController : MonoBehaviour
{
    public GameTypeView GameTypePopUp;
    public JoinView JoinPopUp;
    public JoinView CreatePopUp;
    public GameLogView GameLog;
    public GameView GameTable;
    public BidView BidPopUp;
    public MeldView MeldSheet;
    public TrumpView TrumpPopUp;
    public GameOverView GameOverScreen;
    public GameStoppedView StoppedScreen;

    public static ViewController Instance;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateGameTypes(string[] gameTypes)
    {
        GameTypePopUp.Init(gameTypes);
    }

    public void UpdateAvailableGames(string[] games)
    {
        JoinPopUp.HandleAvailableGames(games);
    }

    public void UpdateLog(string name, string message)
    {
        GameLog.UpdateLog(name, message);
    }

    public void UpdateBidInfo(string bidder, string bid)
    {
        GameLog.SetInfo("Bidder", bidder, 0);
        GameLog.SetInfo("Bid", bid, 1);
    }

    public void UpdateTrumpInfo(string trump)
    {
        GameLog.SetInfo("Trump", trump, 2);
    }

    public void UpdatePassingInfo(string passingTo)
    {
        GameLog.SetInfo("Passing to", passingTo, 0);
    }

    public void UpdateSort(bool reverse)
    {
        GameTable.SetSort(reverse);
    }

    public void ClearInfo()
    {
        GameLog.ClearInfo();
    }

    public void ClearGameLog()
    {
        GameLog.ClearInfo();
        GameLog.ClearLog();
        GameLog.ResetScores();
    }

    public void UpdateScoreInfo(string playerName, int score)
    {
        GameLog.UpdateScore(playerName, score);
    }

    public void HideJoinWindow()
    {
        JoinPopUp.Hide();
        CreatePopUp.Hide();
    }

    public void SetJoinError(string error)
    {
        JoinPopUp.SetError(error);
        CreatePopUp.SetError(error);
    }

    public void ShowGameTypePopUp(bool show)
    {
        GameTypePopUp.gameObject.SetActive(show);
    }

    public void ShowGameTable(bool show)
    {
        GameTable.gameObject.SetActive(show);
    }

    public void ShowBidWindow(bool show, int curBid=0)
    {
        if (show)
        {
            BidPopUp.Init(curBid);
        }
        BidPopUp.gameObject.SetActive(show);
    }

    public void ShowTrumpWindow(bool show)
    {
        TrumpPopUp.gameObject.SetActive(show);
    }

    public void ShowMeldWindow(bool show, MeldPointsMessage meldPointsMessage=null)
    {
        if (meldPointsMessage != null)
        {
            MeldSheet.Reset();
            MeldSheet.SetMeldPoints(meldPointsMessage);
        }
        MeldSheet.gameObject.SetActive(show);
    }

    public void ShowGameOverWindow(bool show, string winningPlayer=null)
    {
        if (show)
        {
            GameOverScreen.SetWinningPlayer(winningPlayer);
        }
        GameOverScreen.gameObject.SetActive(show);
    }

    public void DisableRestart()
    {
        GameOverScreen.RestartButton.interactable = false;
    }

    public void ShowStoppedGame(bool show, string playerName=null)
    {
        if (show)
        {
            StoppedScreen.SetPlayerName(playerName);
        }
        StoppedScreen.gameObject.SetActive(show);
    }

    public void DoOnClear(Action action)
    {
        if (GameTable.IsClearEnabled())
        {
            GameTable.AddOnClearTask(action);
        }
        else
        {
            action.Invoke();
        }
    }

    public void UpdateNames(Dictionary<int, string> playerOrderToNameMap, string thisPlayerName)
    {
        int playerOrder = playerOrderToNameMap.Where(kvp => kvp.Value == thisPlayerName).Select(kvp => kvp.Key).Single();
        GameTable.ShowPlayerNames(playerOrderToNameMap, playerOrder);
        GameLog.SetNames(playerOrderToNameMap);
    }

    public void ShowCardsInHand(List<Card> cards)
    {
        GameTable.ShowCards(cards);
    }

    public void AddCardsInHand(List<Card> cards)
    {
        GameTable.AddCards(cards);
    }

    public void ShowCardsInKitty(Card[] cards)
    {
        GameTable.ShowKittyCards(cards);
    }

    public void ShowPlayedCard(Card card, string playerName, bool isThisPlayer)
    {
        GameTable.ShowPlayedCard(card, playerName, isThisPlayer);
    }

    public void EnablePass(int numCards)
    {
        GameTable.EnablePass(numCards);
    }

    public void EnableDiscardCardsInKitty(int numCards)
    {
        GameTable.EnableDiscardKitty(numCards);
    }

    public void EnableClearCardsInKitty()
    {
        GameTable.EnableClearKitty();
    }

    public void EnableClearPlayedCards()
    {
        GameTable.EnableClearTrick();
    }

    public void EnableSubmitMeld(MeldMessage meldMessage)
    {
        MeldSheet.SetMeld(meldMessage);
    }

    public void EnablePlayCard(Card[] validCards)
    {
        GameTable.EnableTurn(validCards);
    }

    public void HighlightName(string name)
    {
        GameTable.HighlightName(name);
    }

    public void UnHighlightNames()
    {
        GameTable.UnHighlightNames();
    }
}
