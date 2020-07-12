using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinView : MonoBehaviour
{
    public TMP_InputField UserField;
    public TMP_InputField GameField;
    public TMP_Dropdown GameList;
    public Button JoinButton;
    public TextMeshProUGUI ErrorMessage;

    void Start()
    {
        JoinButton.onClick.AddListener(Join);
        ErrorMessage.text = string.Empty;
    }

    public void HandleAvailableGames(string[] availableGames)
    {
        if (GameList != null)
        {
            GameList.options.Clear();
            GameList.AddOptions(new List<string>(availableGames.ToList()));
        }
    }

    public void Hide()
    {
        ErrorMessage.text = string.Empty;
        gameObject.SetActive(false);
    }

    public void SetError(string error)
    {
        ErrorMessage.text = error;
    }

    private void Join()
    {
        string userName = UserField.text.Trim();
        if (GameField != null)
        {
            string gameName = GameField.text.Trim();
            if (userName != string.Empty && gameName != string.Empty)
            {
                Client.Instance.SubmitJoinGame(userName, gameName);
            }
            else
            {
                ErrorMessage.text = "Enter a game name";
            }
        }
        else
        {
            if (userName == string.Empty)
            {
                ErrorMessage.text = "Enter a user name";
            }
            else if (GameList.options.Count == 0)
            {
                ErrorMessage.text = "Select a game";
            }
            else
            {
                Client.Instance.SubmitJoinGame(userName, GameList.options[GameList.value].text);
            }
        }
    }
}
