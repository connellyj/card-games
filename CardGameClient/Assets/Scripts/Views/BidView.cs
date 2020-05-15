using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BidView : MonoBehaviour
{
    public TMP_InputField BidInput;
    public TextMeshProUGUI BidText;
    public TextMeshProUGUI ErrorText;
    public Button BidButton;
    public Button PassButton;

    private int CurBid;

    void Start()
    {
        BidButton.onClick.AddListener(Bid);
        PassButton.onClick.AddListener(Pass);
        ErrorText.text = string.Empty;
    }

    public void Init(int curBid)
    {
        CurBid = curBid;
        BidText.text = curBid.ToString();
        BidInput.text = string.Empty;
        ErrorText.text = string.Empty;
    }

    public void Pass()
    {
        Client.Instance.SubmitBid(CurBid, 0);
    }

    public void Bid()
    {
        if (int.TryParse(BidInput.text.Trim(), out int bid))
        {
            if (bid > CurBid)
            {
                Client.Instance.SubmitBid(CurBid, bid);
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
