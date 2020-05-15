using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameView : MonoBehaviour
{
    // Player names
    public TextMeshProUGUI[] PlayerNameTexts;

    // Card insantiation
    public GameObject CardPrefab;
    public CardView[] PlayedCards;
    public CardView FirstKittyCard;
    public CardView FirstHandCard;
    public CardView SecondHandCard;

    // Card images
    public Sprite ClubImage;
    public Sprite DiamondImage;
    public Sprite SpadeImage;
    public Sprite HeartImage;

    // Buttons
    public Button ClearButton;
    public Button PlayButton;
    public Button DiscardButton;
    public Button PassButton;

    // Data variables
    private Queue<Task> OnClearQueue;
    private Dictionary<CardView, Card> CardViewMap;
    private List<CardView> KittyViews;
    private List<CardView> PlayedViews;
    private List<CardView> SelectedCards;
    private List<CardView> SelectableCards;
    private int NumSelectable;
    private bool Selectable;

    // Card display details
    private Dictionary<string, Sprite> SuitMap;
    private static readonly string RedText = "FF0000";
    private static readonly string BlackText = "000000";
    private static readonly Dictionary<string, string> ColorMap = new Dictionary<string, string>()
    {
        { "C", BlackText }, { "D", RedText }, { "S", BlackText },{ "H", RedText }
    };

    // Name colors
    private static Color NameHighlight = Color.yellow;
    private static Color NameOriginal = Color.white;

    void Start()
    {
        OnClearQueue = new Queue<Task>();
        CardViewMap = new Dictionary<CardView, Card>();
        KittyViews = new List<CardView>();
        PlayedViews = new List<CardView>();
        SelectedCards = new List<CardView>();
        SelectableCards = new List<CardView>();

        // Card display details
        SuitMap = new Dictionary<string, Sprite>()
        {
            { "C", ClubImage }, { "D", DiamondImage }, { "S", SpadeImage },{ "H", HeartImage }
        };

        // set up buttons
        ClearButton.gameObject.SetActive(false);
        ResetButtonsAndSelections();

        DiscardButton.onClick.AddListener(() => 
        {
            IEnumerable<Card> discards = GetSelectedCards();
            RemoveCards(discards);
            Client.Instance.SubmitDiscardKitty(discards);
            KittyViews = new List<CardView>();
        });
        DiscardButton.onClick.AddListener(ResetButtonsAndSelections);

        PlayButton.onClick.AddListener(() => { Client.Instance.SubmitTurn(GetSelectedCards().Single()); });
        PlayButton.onClick.AddListener(ResetButtonsAndSelections);

        PassButton.onClick.AddListener(() =>
        {
            IEnumerable<Card> discards = GetSelectedCards();
            RemoveCards(discards);
            Client.Instance.SubmitPass(discards.ToArray());
        });
        PassButton.onClick.AddListener(ResetButtonsAndSelections);

        ClearButton.onClick.AddListener(ClearPlayedCards);
    }

    public bool IsClearEnabled()
    {
        return ClearButton.IsActive();
    }

    public void AddOnClearTask(Action action)
    {
        OnClearQueue.Enqueue(new Task(action));
    }   
    
    public void HighlightName(string name)
    {
        PlayerNameTexts.Where(p => p.text == name).Single().color = NameHighlight;
    }

    public void UnHighlightNames()
    {
        foreach (TextMeshProUGUI t in PlayerNameTexts)
        {
            t.color = NameOriginal;
        }
    }

    public void ShowPlayerNames(Dictionary<int, string> orderNameMap, int thisPlayerKey)
    {
        // queue in order
        Queue<string> queue = new Queue<string>();
        for (int i = 0; i < orderNameMap.Count; i++)
        {
            queue.Enqueue(orderNameMap[i]);
        }

        // rotate so this player is in front
        while (queue.Peek() != orderNameMap[thisPlayerKey])
        {
            queue.Enqueue(queue.Dequeue());
        }

        // update text
        for (int i = 0; i < orderNameMap.Count; i++)
        {
            PlayerNameTexts[i].text = queue.Dequeue();
        }
    }

    public void ShowCards(List<Card> cards)
    {
        ResetCardsInHand(cards.OrderBy(c => c));
    }

    public void AddCards(List<Card> cards)
    {
        cards.AddRange(CardViewMap.Values);
        ResetCardsInHand(cards);
    }

    public void ShowKittyCards(IEnumerable<Card> kitty)
    {
        AddKittyCards(kitty.OrderBy(c => c));
    }

    public void ShowPlayedCard(Card card, string player, bool destroyExisting)
    {
        Transform tform = PlayerNameTexts
            .Zip(PlayedCards, (t, l) => new { t, l })
            .Where(a => a.t.text == player)
            .Select(a => a.l)
            .Single().transform;

        CardView cardView = Instantiate(CardPrefab, tform.position, tform.rotation, transform).GetComponent<CardView>();
        cardView.Init(card, ColorMap[card.Suit], SuitMap[card.Suit]);
        PlayedViews.Add(cardView);

        if (destroyExisting)
        {
            DestroyCard(card);
        }
    }

    public void EnableTurn(Card[] validCards)
    {
        List<CardView> validCardViews = CardViewMap
            .Where(kvp => validCards.Any(c => c.Equals(kvp.Value)))
            .Select(kvp => kvp.Key).ToList();
        EnableSelect(1, validCardViews);
        PlayButton.gameObject.SetActive(true);
    }

    public void EnablePass(int numCards)
    {
        PassButton.gameObject.SetActive(true);
        EnableSelect(numCards, CardViewMap.Keys.ToList());
    }

    public void EnableDiscardKitty(int numCards)
    {
        DiscardButton.gameObject.SetActive(true);
        EnableSelect(numCards, CardViewMap.Keys.ToList());
    }

    public void EnableClearKitty()
    {
        foreach (CardView cv in KittyViews)
        {
            CardViewMap.Remove(cv);
        }
        EnableClearTrick();
    }

    public void EnableClearTrick()
    {
        ClearButton.gameObject.SetActive(true);
    }

    private void RemoveCards(IEnumerable<Card> cards)
    {
        List<Card> curCards = CardViewMap.Values.ToList();
        foreach (Card c in cards)
        {
            curCards.Remove(c);
        }
        ShowCards(curCards);
    }

    private void ResetCardsInHand(IEnumerable<Card> cards)
    {
        // clear any existing cards
        foreach (CardView cv in CardViewMap.Keys)
        {
            Destroy(cv.gameObject);
        }
        CardViewMap = new Dictionary<CardView, Card>();

        // create new cards
        Vector3 location = FirstHandCard.transform.position;
        foreach (Card card in cards)
        {
            CardView cv = CreateAndRegisterCardView(card, location);
            location.x += GetCardSpacing(cv);
        }
    }

    private void AddKittyCards(IEnumerable<Card> kitty)
    {
        // create new cards
        Vector3 location = FirstKittyCard.transform.position;
        foreach (Card card in kitty)
        {
            CardView cv = CreateAndRegisterCardView(card, location);
            KittyViews.Add(cv);
            location.x += GetCardSpacing(cv);
        }
    }

    private float GetCardSpacing(CardView cv)
    {
        return SecondHandCard.transform.position.x - FirstHandCard.transform.position.x;
    }

    private void DestroyCard(Card card)
    {
        CardView cv = CardViewMap
            .Where(kvp => card.Equals(kvp.Value))
            .Select(kvp => kvp.Key)
            .First();
        Destroy(cv.gameObject);
        CardViewMap.Remove(cv);
    }

    private void ClearPlayedCards()
    {
        if (KittyViews.Count > 0)
        {
            foreach (CardView cv in KittyViews)
            {
                Destroy(cv.gameObject);
            }
            KittyViews = new List<CardView>();
        }
        else
        {
            foreach (CardView cv in PlayedViews)
            {
                Destroy(cv.gameObject);
            }
            PlayedViews = new List<CardView>();
        }

        ClearButton.gameObject.SetActive(false);
        DoClearTasks();
    }

    private void DoClearTasks()
    {
        while (OnClearQueue.Count > 0)
        {
            OnClearQueue.Dequeue().RunSynchronously();
        }
    }

    private void EnableSelect(int numSelectable, List<CardView> selectableCards)
    {
        SelectableCards = selectableCards;
        HighlightSelectableCards(true);
        NumSelectable = numSelectable;
        Selectable = true;
        SelectedCards = new List<CardView>();
    }

    private void HandleCardClicked(CardView cardView)
    {
        if (Selectable && SelectableCards.Contains(cardView))
        {
            if (SelectedCards.Contains(cardView))
            {
                SetInteractable(false);
                SelectedCards.Remove(cardView);
                cardView.Select(false);
            }
            else if (NumSelectable == 1 && SelectedCards.Count == 1)
            {
                SelectedCards[0].Select(false);
                SelectedCards.Clear();
                SelectedCards.Add(cardView);
                cardView.Select(true);
            }
            else if (SelectedCards.Count < NumSelectable)
            {
                SetInteractable(false);
                SelectedCards.Add(cardView);
                cardView.Select(true);
            }

            if (SelectedCards.Count == NumSelectable)
            {
                SetInteractable(true);
            }
        }
    }

    private CardView CreateAndRegisterCardView(Card card, Vector3 location)
    {
        GameObject cardObject = Instantiate(CardPrefab, location, Quaternion.identity, transform);
        CardView cardView = cardObject.GetComponent<CardView>();
        cardView.Init(card, ColorMap[card.Suit], SuitMap[card.Suit]);
        cardView.RegisterSelectListener(() => HandleCardClicked(cardView));
        CardViewMap.Add(cardView, card);
        return cardView;
    }

    private IEnumerable<Card> GetSelectedCards()
    {
        return CardViewMap.Where(kvp => SelectedCards.Contains(kvp.Key)).Select(kvp => kvp.Value);
    }

    private void HighlightSelectableCards(bool highlight)
    {
        foreach (CardView cv in SelectableCards)
        {
            cv.Highlight(highlight);
        }
    }

    private void SetInteractable(bool interactable)
    {
        PlayButton.interactable = interactable;
        DiscardButton.interactable = interactable;
        PassButton.interactable = interactable;
    }

    private void DisableButtons()
    {
        SetInteractable(false);
        PlayButton.gameObject.SetActive(false);
        DiscardButton.gameObject.SetActive(false);
        PassButton.gameObject.SetActive(false);
    }

    private void ResetButtonsAndSelections()
    {
        // buttons
        DisableButtons();

        // unhighlight cards
        HighlightSelectableCards(false);
        NumSelectable = 0;
        Selectable = false;
        SelectableCards = new List<CardView>();
    }
}
