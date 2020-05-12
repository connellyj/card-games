using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ViewController : MonoBehaviour
{
    public GameTypeView GameTypePopUp;
    public JoinView JoinPopUp;
    public GameLogView GameLog;
    public GameView GameTable;
    public BidView BidPopUp;
    public MeldView MeldSheet;
    public TrumpView TrumpPopUp;
    public GameObject GameOverScreen;
    public TextMeshProUGUI WinningPlayerText;

    public static ViewController Instance;

    private string WinningPlayer;
    private bool BidEnabled;
    private bool MeldEnabled;
    private bool TrumpEnabled;
    private bool GameOverEnabled;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        BidEnabled = false;
        MeldEnabled = false;
        TrumpEnabled = false;
        GameOverEnabled = false;
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
        GameLog.SetBidder(bidder);
        GameLog.SetBid(bid);
    }

    public void UpdateTrumpInfo(string trump)
    {
        GameLog.SetTrump(trump);
    }

    public void UpdateScoreInfo(string playerName, int score)
    {
        GameLog.UpdateScore(playerName, score);
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
        BidEnabled = show;
    }

    public void ShowTrumpWindow(bool show)
    {
        TrumpEnabled = show;
    }

    public void ShowMeldWindow(bool show, MeldPointsMessage meldPointsMessage=null)
    {
        if (meldPointsMessage != null)
        {
            MeldSheet.SetMeldPoints(meldPointsMessage);
        }
        MeldEnabled = show;
    }

    public void ShowGameOverWindow(bool show, string winningPlayer)
    {
        if (show)
        {
            WinningPlayer = winningPlayer;
        }
        GameOverEnabled = show;
    }

    public void WaitForCardsCleared()
    {
        GameTable.WaitForCardsCleared();
    }

    public void UpdateNames(Dictionary<int, string> playerOrderToNameMap, string thisPlayerName)
    {
        // Wait for the view to initalize
        GameTable.WaitForInitialization();

        // Fill in names on table and log
        int playerOrder = playerOrderToNameMap.Where(kvp => kvp.Value == thisPlayerName).Select(kvp => kvp.Key).Single();
        GameTable.ShowPlayerNames(playerOrderToNameMap, playerOrder);
        GameLog.SetNames(playerOrderToNameMap);
    }

    public void ShowCardsInHand(List<Card> cards)
    {
        GameTable.ShowCards(cards);
    }

    public void ShowCardsInKitty(Card[] cards)
    {
        GameTable.ShowKittyCards(cards);
    }

    public void ShowPlayedCard(Card card, string playerName, bool isThisPlayer)
    {
        GameTable.ShowPlayedCard(card, playerName, isThisPlayer);
    }

    public void EnableDiscardCardsInKitty(int numCards)
    {
        GameTable.HandleDiscardKitty(numCards);
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
}
