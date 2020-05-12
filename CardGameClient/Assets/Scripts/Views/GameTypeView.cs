using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameTypeView : MonoBehaviour
{
    public TMP_Dropdown GameTypeDropdown;
    public TextMeshProUGUI GameNameText;
    public Button ActionButton;

    private string[] Options;

    void Start()
    {
        ActionButton.onClick.AddListener(UpdateGameType);
    }

    void Update()
    {
        if (Options != null)
        {
            GameTypeDropdown.AddOptions(Options.ToList());
            Options = null;
        }
    }

    public void Init(string[] gameTypes)
    {
        Options = gameTypes;
    }

    private void UpdateGameType()
    {
        string gameType = GameTypeDropdown.options[GameTypeDropdown.value].text;
        GameNameText.text = gameType;
        gameObject.SetActive(false);
        Client.Instance.SubmitGameType(gameType);
    }
}
