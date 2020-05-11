using System.Collections.Generic;
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

    private List<string> GameOptions;

    void Start()
    {
        JoinButton.onClick.AddListener(Join);
        ErrorMessage.text = string.Empty;
    }

    void Update()
    {
        if (GameOptions != null)
        {
            if (GameList != null)
            {
                GameList.options.Clear();
                GameList.AddOptions(new List<string>(GameOptions));
            }
            GameOptions = null;
        }
    }

    public void HandleAvailableGames(string[] availableGames)
    {
        GameOptions = new List<string>(availableGames);
    }

    private void Join()
    {
        Response response = null;
        string userName = UserField.text.Trim();
        if (GameField != null)
        {
            string gameName = GameField.text.Trim();
            if (userName != string.Empty && gameName != string.Empty)
            {
                response = Client.Instance.SubmitJoinGame(userName, gameName);
            }
            else
            {
                ErrorMessage.text = "Enter a game name";
            }
        }
        else
        {
            if (userName != string.Empty && GameList.options.Count > 0)
            {
                response = Client.Instance.SubmitJoinGame(userName, GameList.options[GameList.value].text);
            }
            else
            {
                ErrorMessage.text = "Enter a user name";
            }
        }

        if (response != null && response.Success)
        {
            ErrorMessage.text = string.Empty;
            gameObject.SetActive(false);
        }
        else if (response != null)
        {
            ErrorMessage.text = response.ErrorMessage;
        }
    }
}
