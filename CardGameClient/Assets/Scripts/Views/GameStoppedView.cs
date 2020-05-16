using TMPro;
using UnityEngine;

public class GameStoppedView : MonoBehaviour
{
    public TextMeshProUGUI PlayerNameText;

    public void SetPlayerName(string name)
    {
        PlayerNameText.text = name;
    }
}
