using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrumpView : MonoBehaviour
{
    public GameObject Clubs;
    public GameObject Diamonds;
    public GameObject Spades;
    public GameObject Hearts;
    public Button OkButton;

    private GameObject Selected;
    private Dictionary<GameObject, string> TrumpMap;

    void Start()
    {
        TrumpMap = new Dictionary<GameObject, string>();
        TrumpMap.Add(Clubs, "C");
        TrumpMap.Add(Diamonds, "D");
        TrumpMap.Add(Spades, "S");
        TrumpMap.Add(Hearts, "H");
        Clubs.GetComponent<Button>().onClick.AddListener(() => ToggleHighlight(Clubs));
        Diamonds.GetComponent<Button>().onClick.AddListener(() => ToggleHighlight(Diamonds));
        Spades.GetComponent<Button>().onClick.AddListener(() => ToggleHighlight(Spades));
        Hearts.GetComponent<Button>().onClick.AddListener(() => ToggleHighlight(Hearts));
        OkButton.onClick.AddListener(HandleTrump);
    }

    private void HandleTrump()
    {
        if (Selected != null)
        {
            Client.Instance.HandleTrump(TrumpMap[Selected]);
        }
    }

    private void ToggleHighlight(GameObject obj)
    {
        if (Selected == obj)
        {
            Selected = null;
            obj.gameObject.GetComponent<Outline>().enabled = false;
        }
        else
        {
            if (Selected != null)
            {
                Selected.gameObject.GetComponent<Outline>().enabled = false;
            }
            Selected = obj;
            obj.gameObject.GetComponent<Outline>().enabled = true;
        }
    }
}
