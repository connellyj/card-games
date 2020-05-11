using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class GameLogView : MonoBehaviour
{
    public ScrollRect ScrollRect;
    public TextMeshProUGUI Log;
    public TextMeshProUGUI BidderText;
    public TextMeshProUGUI BidText;
    public TextMeshProUGUI TrumpText;
    public TextMeshProUGUI Player1Text;
    public TextMeshProUGUI Player2Text;
    public TextMeshProUGUI Player3Text;
    public TextMeshProUGUI Player1ScoreText;
    public TextMeshProUGUI Player2ScoreText;
    public TextMeshProUGUI Player3ScoreText;

    private string UpdateLogString;
    private string Bidder;
    private string Bid;
    private string Trump;
    private string Player1;
    private string Player2;
    private string Player3;

    private Dictionary<string, string> ScoreMap = new Dictionary<string, string>();  // init here for lock purposes
    private Dictionary<int, string> OrderMap = new Dictionary<int, string>();  // init here for lock purposes

    void Start()
    {
        lock (ScoreMap)
        {
            ScoreMap = new Dictionary<string, string>();
        }
        lock (OrderMap)
        {
            OrderMap = new Dictionary<int, string>();
        }
        ScrollRect.verticalNormalizedPosition = 0;
    }

    void Update()
    {
        if (UpdateLogString != string.Empty)
        {
            Log.text += UpdateLogString;
            UpdateLogString = string.Empty;
        }
        if (Bidder != string.Empty)
        {
            BidderText.text = Bidder;
            Bidder = string.Empty;
        }
        if (Bid != string.Empty)
        {
            BidText.text = Bid;
            Bid = string.Empty;
        }
        if (Trump != string.Empty)
        {
            TrumpText.text = Trump;
            Trump = string.Empty;
        }
        if (Player1 != string.Empty)
        {
            Player1Text.text = Player1;
            Player1 = string.Empty;
        }
        if (Player2 != string.Empty)
        {
            Player2Text.text = Player2;
            Player2 = string.Empty;
        }
        if (Player3 != string.Empty)
        {
            Player3Text.text = Player3;
            Player3 = string.Empty;
        }
        lock (ScoreMap)
        {
            lock (OrderMap)
            {
                foreach (int i in OrderMap.Keys)
                {
                    if (ScoreMap[OrderMap[i]] != string.Empty)
                    {
                        if (i == 0)
                        {
                            Player1ScoreText.text = ScoreMap[OrderMap[i]];
                        }
                        else if (i == 1)
                        {
                            Player2ScoreText.text = ScoreMap[OrderMap[i]];
                        }
                        else if (i == 2)
                        {
                            Player3ScoreText.text = ScoreMap[OrderMap[i]];
                        }
                        ScoreMap[OrderMap[i]] = string.Empty;
                    }
                }
            }
        }
    }

    public void UpdateLog(string name, string message)
    {
        string log = name + ": " + message + "\n";
        UpdateLogString += log;
    }

    public void SetBidder(string bidder)
    {
        Bidder = bidder;
    }

    public void SetBid(string bid)
    {
        Bid = bid;
    }

    public void SetTrump(string trump)
    {
        Trump = trump;
    }

    public void SetNames(Dictionary<int, string> names)
    {
        Player1 = names[0];
        Player2 = names[1];
        Player3 = names[2];
        lock (ScoreMap)
        {
            ScoreMap.Add(Player1, "0");
            ScoreMap.Add(Player2, "0");
            ScoreMap.Add(Player3, "0");
        }
        lock (OrderMap)
        {
            OrderMap.Add(0, Player1);
            OrderMap.Add(1, Player2);
            OrderMap.Add(2, Player3);
        }
    }

    public void UpdateScore(string player, int score)
    {
        lock (ScoreMap)
        {
            ScoreMap[player] = score.ToString();
        }
    }
}
