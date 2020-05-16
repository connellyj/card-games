using System.Collections;
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

    private Dictionary<string, int> PlayerScoreMap;

    void Start()
    {
        PlayerScoreMap = new Dictionary<string, int>();
        ScrollRect.verticalNormalizedPosition = 0;
    }

    void Update()
    {
        if (UpdateLogString != string.Empty)
        {
            Log.text += UpdateLogString;
            UpdateLogString = string.Empty;
            StartCoroutine(ScrollToBottom());
        }
    }

    public void UpdateLog(string name, string message)
    {
        if (name.Length > 11)
        {
            name = name.Substring(0, 11) + "...";
        }
        string log = "<b>" + name + "</b>: " + message + "\n";
        UpdateLogString += log;
    }

    public void ClearLog()
    {
        Log.text = string.Empty;
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
        ResetScores();
    }

    public void UpdateScore(string player, int score)
    {
        ScoreTexts[PlayerScoreMap[player]].text = score.ToString();
    }

    public void ResetScores()
    {
        foreach (TextMeshProUGUI t in ScoreTexts)
        {
            t.text = "0";
        }
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        ScrollRect.verticalNormalizedPosition = 0;
        yield return null;
    }
}
