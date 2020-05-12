using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    public TextMeshProUGUI UpperText;
    public TextMeshProUGUI LowerText;
    public Image UpperImage;
    public Image LowerImage;
    public Color SelectedColor;
    public Color NormalColor;
    public Color HighlightColor;

    private bool Highlighted;

    public void Init(Card card, string color, Sprite image)
    {
        string s = "<color=#" + color + ">" + card.Rank + "</color>";
        UpperText.text = s;
        LowerText.text = s;
        UpperImage.sprite = image;
        LowerImage.sprite = image;
        Highlighted = false;
    }

    public void RegisterSelectListener(UnityEngine.Events.UnityAction listener)
    {
        gameObject.GetComponent<Button>().onClick.AddListener(listener);
    }

    public void Select(bool toggle)
    {
        gameObject.GetComponent<Outline>().effectColor = toggle ? SelectedColor : (Highlighted ? HighlightColor : NormalColor);
    }

    public void Highlight(bool highlight)
    {
        Highlighted = highlight;
        gameObject.GetComponent<Outline>().effectColor = highlight ? HighlightColor : NormalColor;
    }
}
