using System.Collections.Generic;
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

    private Dictionary<string, int> PlayerScoreMap;

    void Start()
    {
        PlayerNames = new string[PlayerNameTexts.Length];
        PlayerScoreMap = new Dictionary<string, int>();
        ScrollRect.verticalNormalizedPosition = 0;
    }

    void Update()
    {
        if (UpdateLogString != string.Empty)
        {
            Log.text += UpdateLogString;
            UpdateLogString = string.Empty;
        }
    }

    public void UpdateLog(string name, string message)
    {
        string log = name + ": " + message + "\n";
        UpdateLogString += log;
    }

    public void SetInfo(string name, string value, int index)
    {
        InfoTexts[index].text = name + ": " + value;
    }

    public void ClearInfo()
    {
        for (int i = 0; i < InfoTexts.Length; i++)
        {
            InfoTexts[i].text = string.Empty;
        }
    }

    public void SetNames(Dictionary<int, string> names)
    {
        foreach (int key in names.Keys)
        {
            PlayerNameTexts[key].text = names[key];
            PlayerScoreMap.Add(names[key], key);
        }
    }

    public void UpdateScore(string player, int score)
    {
        ScoreTexts[PlayerScoreMap[player]].text = score.ToString();
    }
}
