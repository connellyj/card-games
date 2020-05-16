using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MeldView : MonoBehaviour
{
    public TMP_Dropdown JacksAround;
    public TMP_Dropdown QueensAround;
    public TMP_Dropdown KingsAround;
    public TMP_Dropdown AcesAround;
    public TMP_Dropdown ClubsMarriage;
    public TMP_Dropdown DiamondsMarriage;
    public TMP_Dropdown SpadesMarriage;
    public TMP_Dropdown HeartsMarriage;
    public TMP_Dropdown Runs;
    public TMP_Dropdown Nines;
    public TMP_Dropdown Pinochles;
    public TextMeshProUGUI TotalText;
    public Button SubmitButton;
    public TextMeshProUGUI ErrorText;

    private MeldPointsMessage MeldPoints;
    private MeldMessage Meld;

    void Start()
    {
        Reset();
        SubmitButton.onClick.AddListener(SubmitMeld);
        JacksAround.onValueChanged.AddListener((int pos) => UpdateTotal());
        QueensAround.onValueChanged.AddListener((int pos) => UpdateTotal());
        KingsAround.onValueChanged.AddListener((int pos) => UpdateTotal());
        AcesAround.onValueChanged.AddListener((int pos) => UpdateTotal());
        ClubsMarriage.onValueChanged.AddListener((int pos) => UpdateTotal());
        DiamondsMarriage.onValueChanged.AddListener((int pos) => UpdateTotal());
        SpadesMarriage.onValueChanged.AddListener((int pos) => UpdateTotal());
        HeartsMarriage.onValueChanged.AddListener((int pos) => UpdateTotal());
        Runs.onValueChanged.AddListener((int pos) => UpdateTotal());
        Nines.onValueChanged.AddListener((int pos) => UpdateTotal());
        Pinochles.onValueChanged.AddListener((int pos) => UpdateTotal());
    }

    public void Reset()
    {
        Meld = null;
        JacksAround.value = 0;
        QueensAround.value = 0;
        KingsAround.value = 0;
        AcesAround.value = 0;
        ClubsMarriage.value = 0;
        DiamondsMarriage.value = 0;
        SpadesMarriage.value = 0;
        HeartsMarriage.value = 0;
        Runs.value = 0;
        Nines.value = 0;
        Pinochles.value = 0;
        TotalText.text = "0";
        SubmitButton.interactable = false;
        ErrorText.gameObject.SetActive(false);
    }

    public void SetMeldPoints(MeldPointsMessage meld)
    {
        MeldPoints = meld;
    }

    public void SetMeld(MeldMessage meld)
    {
        SubmitButton.interactable = true;
        Meld = meld;
        UpdateTotal();
    }

    private void SubmitMeld()
    {
        if (!IsValid())
        {
            ErrorText.gameObject.SetActive(true);
        }
        else
        {
            Client.Instance.SubmitMeld(Meld);
        }
    }

    private bool IsValid()
    {
        return Meld.JacksAround == int.Parse(JacksAround.options[JacksAround.value].text) &&
            Meld.QueensAround == int.Parse(QueensAround.options[QueensAround.value].text) &&
            Meld.KingsAround == int.Parse(KingsAround.options[KingsAround.value].text) &&
            Meld.AcesAround == int.Parse(AcesAround.options[AcesAround.value].text) &&
            Meld.ClubsMarriage == int.Parse(ClubsMarriage.options[ClubsMarriage.value].text) &&
            Meld.DiamondsMarriage == int.Parse(DiamondsMarriage.options[DiamondsMarriage.value].text) &&
            Meld.SpadesMarriage == int.Parse(SpadesMarriage.options[SpadesMarriage.value].text) &&
            Meld.HeartsMarriage == int.Parse(HeartsMarriage.options[HeartsMarriage.value].text) &&
            Meld.Pinochle == int.Parse(Pinochles.options[Pinochles.value].text) &&
            Meld.TrumpNine == int.Parse(Nines.options[Nines.value].text) &&
            Meld.Run == int.Parse(Runs.options[Runs.value].text);
    }

    private void UpdateTotal()
    {
        int total = 0;
        total += GetAroundPoints(JacksAround, MeldPoints.JacksAroundPoints);
        total += GetAroundPoints(QueensAround, MeldPoints.QueensAroundPoints);
        total += GetAroundPoints(KingsAround, MeldPoints.KingsAroundPoints);
        total += GetAroundPoints(AcesAround, MeldPoints.AcesAroundPoints);
        total += GetAroundPoints(Runs, MeldPoints.RunPoints);
        total += GetNumMarriages() * MeldPoints.MarriagePoints;
        total += GetPinochlePoints();
        total += int.Parse(Nines.options[Nines.value].text) * MeldPoints.TrumpNinePoints;
        if (Meld != null)
        {
            int numMarriages = 0;
            if (Meld.Trump == "C")
            {
                numMarriages = int.Parse(ClubsMarriage.options[ClubsMarriage.value].text);
            }
            else if (Meld.Trump == "D")
            {
                numMarriages = int.Parse(DiamondsMarriage.options[DiamondsMarriage.value].text);
            }
            else if (Meld.Trump == "S")
            {
                numMarriages = int.Parse(SpadesMarriage.options[SpadesMarriage.value].text);
            }
            else if (Meld.Trump == "H")
            {
                numMarriages = int.Parse(HeartsMarriage.options[HeartsMarriage.value].text);
            }
            total -= MeldPoints.MarriagePoints * numMarriages;
            total += MeldPoints.TrumpMarriagePoints * numMarriages;
        }

        TotalText.text = total.ToString();
    }

    private int GetAroundPoints(TMP_Dropdown dropdown, int points)
    {
        if (dropdown.options[dropdown.value].text == "2")
        {
            return points * MeldPoints.AroundMultiplierPoints;
        }
        else if (dropdown.options[dropdown.value].text == "1")
        {
            return points;
        }
        else
        {
            return 0;
        }
    }

    private int GetPinochlePoints()
    {
        if (Pinochles.options[Pinochles.value].text == "2")
        {
            return MeldPoints.DoublePinochlePoints;
        }
        else if (Pinochles.options[Pinochles.value].text == "1")
        {
            return MeldPoints.PinochlePoints;
        }
        else
        {
            return 0;
        }
    }

    private int GetNumMarriages()
    {
        return int.Parse(ClubsMarriage.options[ClubsMarriage.value].text) +
            int.Parse(DiamondsMarriage.options[DiamondsMarriage.value].text) +
            int.Parse(SpadesMarriage.options[SpadesMarriage.value].text) +
            int.Parse(HeartsMarriage.options[HeartsMarriage.value].text);
    }
}
