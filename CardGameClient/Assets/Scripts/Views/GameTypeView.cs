using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameTypeView : MonoBehaviour
{
    public TMP_Dropdown GameTypeDropdown;
    public TextMeshProUGUI GameNameText;
    public Button ActionButton;
    public Toggle ReverseCheckbox;

    void Start()
    {
        ActionButton.onClick.AddListener(UpdateGameType);
    }

    public void Init(string[] gameTypes)
    {
        GameTypeDropdown.AddOptions(gameTypes.ToList());
    }

    private void UpdateGameType()
    {
        string gameType = GameTypeDropdown.options[GameTypeDropdown.value].text;
        GameNameText.text = gameType;
        gameObject.SetActive(false);
        Client.Instance.SubmitGameType(gameType);
        Client.Instance.SubmitSettings(ReverseCheckbox.isOn);
    }
}
