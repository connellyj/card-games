using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameLogView : MonoBehaviour
{
    public ScrollRect ScrollRect;
    public TextMeshProUGUI Log;
    public TextMeshProUGUI[] PlayerNameTexts;
    public TextMeshProUGUI[] ScoreTexts;
    public TextMeshProUGUI[] InfoTexts;

    private string UpdateLogString;
    private string[] PlayerNames;
    private string[] Info;

    private Dictionary<string, string> ScoreMap = new Dictionary<string, string>();  // init here for lock purposes

    void Start()
    {
        Info = new string[InfoTexts.Length];
        PlayerNames = new string[PlayerNameTexts.Length];
        lock (ScoreMap)
        {
            ScoreMap = new Dictionary<string, string>();
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
        for (int i = 0; i < Info.Length; i++)
        {
            if (Info[i] != string.Empty)
            {
                InfoTexts[i].text = Info[i];
                Info[i] = string.Empty;
            }
        }
        lock (ScoreMap)
        {
            for (int i = 0; i < ScoreMap.Count; i++)
            {
                if (ScoreMap[PlayerNames[i]] != string.Empty)
                {
                    ScoreTexts[i].text = ScoreMap[PlayerNames[i]];
                    ScoreMap[PlayerNames[i]] = string.Empty;
                }
            }
        }
    }

    public void UpdateLog(string name, string message)
    {
        string log = name + ": " + message + "\n";
        UpdateLogString += log;
    }

    public void SetInfo(string name, string value, int index)
    {
        Info[index] = name + ": " + value;
    }

    public void ClearInfo()
    {
        for (int i = 0; i < Info.Length; i++)
        {
            Info[i] = "  ";
        }
    }

    public void SetNames(Dictionary<int, string> names)
    {
        lock (ScoreMap)
        {
            foreach (int key in names.Keys)
            {
                PlayerNames[key] = names[key];
                ScoreMap.Add(names[key], "0");
            }
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
