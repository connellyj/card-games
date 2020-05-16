using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameStoppedView : MonoBehaviour
{
    public TextMeshProUGUI PlayerNameText;
    public Button NewGameButton;

    void Start()
    {
        NewGameButton.onClick.AddListener(() => Client.Instance.SubmitRestart(true));
    }

    public void SetPlayerName(string name)
    {
        PlayerNameText.text = name;
    }
}
