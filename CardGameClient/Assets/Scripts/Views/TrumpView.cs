using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrumpView : MonoBehaviour
{
    public GameObject Clubs;
    public GameObject Diamonds;
    public GameObject Spades;
    public GameObject Hearts;
    public GameObject[] ExtraOptions;
    public Button OkButton;

    private GameObject Selected;
    private Dictionary<GameObject, string> TrumpMap;

    public void Init()
    {
        TrumpMap = new Dictionary<GameObject, string>
        {
            { Clubs, "C" },
            { Diamonds, "D" },
            { Spades, "S" },
            { Hearts, "H" }
        };
        Clubs.GetComponent<Button>().onClick.AddListener(() => ToggleHighlight(Clubs));
        Diamonds.GetComponent<Button>().onClick.AddListener(() => ToggleHighlight(Diamonds));
        Spades.GetComponent<Button>().onClick.AddListener(() => ToggleHighlight(Spades));
        Hearts.GetComponent<Button>().onClick.AddListener(() => ToggleHighlight(Hearts));
        OkButton.onClick.AddListener(HandleTrump);
        foreach (GameObject g in TrumpMap.Select(kvp => kvp.Key))
        {
            g.SetActive(true);
        }
    }

    public void AddExtraOptions(string[] extraOptions)
    {
        for (int i = 0; i < extraOptions.Length; i++)
        {
            TrumpMap.Add(ExtraOptions[i], extraOptions[i]);
            ExtraOptions[i].GetComponentInChildren<TextMeshProUGUI>().text = extraOptions[i];
            GameObject g = ExtraOptions[i];
            ExtraOptions[i].GetComponent<Button>().onClick.AddListener(() => ToggleHighlight(g));
        }
    }

    public void DisableOptions(string[] options)
    {
        foreach (GameObject g in TrumpMap.Where(kvp => options.Contains(kvp.Value)).Select(kvp => kvp.Key))
        {
            g.SetActive(false);
        }
    }

    private void HandleTrump()
    {
        if (Selected != null)
        {
            Client.Instance.SubmitTrump(TrumpMap[Selected]);
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
