using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverView : MonoBehaviour
{
    public Button ExitButton;
    public Button RestartButton;
    public Button NewGameButton;

    public TextMeshProUGUI WinningPlayerText;

    void Start()
    {
        ExitButton.onClick.AddListener(() => Application.Quit());
        RestartButton.onClick.AddListener(Client.Instance.SubmitRestart);
    }

    public void SetWinningPlayer(string winningPlayer)
    {
        WinningPlayerText.text = winningPlayer;
    }
}
