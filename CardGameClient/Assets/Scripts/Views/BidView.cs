using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BidView : MonoBehaviour
{
    public TMP_InputField BidInput;
    public TextMeshProUGUI BidText;
    public TextMeshProUGUI ErrorText;
    public Button Button;

    private int CurBid;

    void Start()
    {
        Button.onClick.AddListener(Bid);
        ErrorText.text = string.Empty;
    }

    public void Init(int curBid)
    {
        CurBid = curBid;
        BidText.text = curBid.ToString();
    }

    public void Bid()
    {
        if (int.TryParse(BidInput.text.Trim(), out int bid))
        {
            if (bid > CurBid || bid == 0)
            {
                Client.Instance.SubmitBid(CurBid, bid);
                ErrorText.text = string.Empty;
            }
            else
            {
                ErrorText.text = "Bid must be higher than the current bid";
            }
        }
        else
        {
            ErrorText.text = "Bids must be an integer";
        }
    }
}
