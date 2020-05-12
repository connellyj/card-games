using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameView : MonoBehaviour
{
    // Player names
    public TextMeshProUGUI[] PlayerNameTexts;
    private string[] PlayerNames;

    // Card insantiation
    public GameObject CardPrefab;
    public Vector2 FirstCardLocation;
    public Vector2 FirstKittyCardLocation;
    public Vector2[] PlayedCardLocations;
    public int CardSpacing;

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
    private bool ClearEnabled;
    private bool PlayEnabled;
    private bool DiscardEnabled;
    private bool PassEnabled;

    // Main thread helper variables
    private List<Card> CardsInHand;
    private List<Card> KittyCards;
    private List<Card> PlayedCards;
    private List<Vector3> PlayedLocations;
    private Card PlayedCardToDestroy;
    private int NumPlayedCards;
    private bool DoHighlight;

    // Data variables
    private Dictionary<CardView, Card> CardViewMap;
    private List<CardView> PlayedViews;
    private List<CardView> SelectedCards;
    private List<CardView> SelectableCards;
    private int NumSelectable;
    private bool Selectable;
    private bool StartDone = false;  // set here to ensure false before object made active

    // Card display details
    private Dictionary<string, Sprite> SuitMap;
    private static readonly string RedText = "FF0000";
    private static readonly string BlackText = "000000";
    private static readonly Dictionary<string, string> ColorMap = new Dictionary<string, string>()
    {
        { "C", BlackText }, { "D", RedText }, { "S", BlackText },{ "H", RedText }
    };

    void Start()
    {
        // Main thread helper variables
        CardsInHand = new List<Card>();
        KittyCards = new List<Card>();
        PlayedCards = new List<Card>();
        PlayedLocations = new List<Vector3>();
        PlayedCardToDestroy = null;
        NumPlayedCards = -1;

        // Data variables
        CardViewMap = new Dictionary<CardView, Card>();
        PlayedViews = new List<CardView>();
        SelectedCards = new List<CardView>();
        SelectableCards = new List<CardView>();

        // Card display details
        SuitMap = new Dictionary<string, Sprite>()
        {
            { "C", ClubImage }, { "D", DiamondImage }, { "S", SpadeImage },{ "H", HeartImage }
        };

        // set up buttons
        ResetButtonsAndSelections();
        DiscardButton.onClick.AddListener(() => 
        {
            IEnumerable<Card> discards = GetSelectedCards();
            RemoveCards(discards);
            Client.Instance.SubmitDiscardKitty(discards);
            lock (PlayedViews)
            {
                PlayedViews = new List<CardView>();
            }
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

        // trigger start done
        Monitor.Enter(this);
        StartDone = true;
        Monitor.PulseAll(this);
        Monitor.Exit(this);
    }

    void Update()
    {
        // show cards in hand
        lock (CardsInHand)
        {
            if (CardsInHand.Count > 0)
            {
                ResetCardsInHand();
                CardsInHand.Clear();
            }
        }

        // show kitty cards
        Monitor.Enter(KittyCards);
        if (KittyCards.Count > 0)
        {
            AddKittyCards();
            KittyCards.Clear();
            Monitor.PulseAll(KittyCards);
        }
        Monitor.Exit(KittyCards);

        // destroy card played by this player
        lock (PlayedCards)
        {
            if (PlayedCardToDestroy != null)
            {
                DestroyPlayedCard();
                PlayedCardToDestroy = null;
            }
        }

        // show newly played cards
        CheckAndCreatePlayedCards();

        // highlight selectable cards
        if (DoHighlight)
        {
            lock (SelectableCards)
            {
                DoHighlight = false;
            }
            HighlightSelectableCards(true);
        }

        // buttons
        if (DiscardEnabled != DiscardButton.gameObject.activeInHierarchy)
        {
            DiscardButton.gameObject.SetActive(DiscardEnabled);
        }
        if (PlayEnabled != PlayButton.gameObject.activeInHierarchy)
        {
            PlayButton.gameObject.SetActive(PlayEnabled);
        }
        if (PassEnabled != PassButton.gameObject.activeInHierarchy)
        {
            PassButton.gameObject.SetActive(PassEnabled);
        }
        if (ClearEnabled != ClearButton.gameObject.activeInHierarchy)
        {
            ClearButton.gameObject.SetActive(ClearEnabled);
        }

        // names
        if (PlayerNames != null)
        {
            for (int i = 0; i < PlayerNames.Length; i++)
            {
                PlayerNameTexts[i].text = PlayerNames[i];
            }
            PlayerNames = null;
        }
    }

    public void ShowPlayerNames(Dictionary<int, string> orderNameMap, int thisPlayerKey)
    {
        WaitForInitialization();

        Queue<string> queue = new Queue<string>();
        for (int i = 0; i < orderNameMap.Count; i++)
        {
            queue.Enqueue(orderNameMap[i]);
        }
        while (queue.Peek() != orderNameMap[thisPlayerKey])
        {
            queue.Enqueue(queue.Dequeue());
        }
        PlayerNames = queue.ToArray();
    }

    public void ShowCards(List<Card> cards)
    {
        WaitForInitialization();

        lock (CardsInHand)
        {
            CardsInHand = cards.OrderBy(c => c).ToList();
        }
    }

    public void AddCards(List<Card> cards)
    {
        WaitForInitialization();

        lock (CardsInHand)
        {
            CardsInHand = cards;
            CardsInHand.AddRange(CardViewMap.Values);
        }
    }

    public void ShowKittyCards(IEnumerable<Card> kitty)
    {
        WaitForInitialization();

        lock (KittyCards)
        {
            KittyCards = kitty.OrderBy(c => c).ToList();
        }
    }

    public void ShowPlayedCard(Card card, string player, bool destroyExisting)
    {
        WaitForInitialization();

        Vector2 location2D = PlayerNameTexts
            .Zip(PlayedCardLocations, (t, l) => new { t, l })
            .Where(a => a.t.text == player)
            .Select(a => a.l)
            .Single();
        Vector3 location = new Vector3(location2D[0], location2D[1], 0);
        lock (PlayedCards)
        {
            PlayedCards.Add(card);
            PlayedLocations.Add(location);
            if (destroyExisting)
            {
                PlayedCardToDestroy = card;
            }
        }
    }

    public void EnableTurn(Card[] validCards)
    {
        WaitForInitialization();
        WaitForHandCreated();

        lock (CardViewMap)
        {
            lock (PlayedViews)
            {
                List<CardView> validCardViews = CardViewMap
                    .Where(kvp => validCards.Any(c => c.Equals(kvp.Value)))
                    .Select(kvp => kvp.Key).ToList();
                EnableSelect(1, validCardViews);
            }
        }
        PlayEnabled = true;
    }

    public void EnablePass(int numCards)
    {
        WaitForInitialization();
        WaitForHandCreated();

        PassEnabled = true;
        EnableSelect(numCards, CardViewMap.Keys.ToList());
    }

    public void EnableDiscardKitty(int numCards)
    {
        WaitForInitialization();
        WaitForKittyCreated();

        DiscardEnabled = true;
        EnableSelect(numCards, CardViewMap.Keys.ToList());
    }

    public void EnableClearKitty()
    {
        WaitForInitialization();
        WaitForKittyCreated();

        lock (CardViewMap)
        {
            foreach (CardView cv in PlayedViews)
            {
                CardViewMap.Remove(cv);
            }
        }
        EnableClearTrick();
    }

    public void EnableClearTrick()
    {
        ClearEnabled = true;
    }

    public void WaitForCardsCleared()
    {
        Monitor.Enter(this);
        while (ClearEnabled)
        {
            Monitor.Wait(this);
        }
        Monitor.Exit(this);
    }

    private void WaitForInitialization()
    {
        Monitor.Enter(this);
        while (!StartDone)
        {
            Monitor.Wait(this);
        }
        Monitor.Exit(this);
    }

    private void WaitForKittyCreated()
    {
        Monitor.Enter(KittyCards);
        while (KittyCards.Count() > 0)
        {
            Monitor.Wait(KittyCards);
        }
        Monitor.Exit(KittyCards);
    }

    private void WaitForHandCreated()
    {
        Monitor.Enter(CardsInHand);
        while (CardsInHand.Count > 0)
        {
            Monitor.Wait(CardsInHand);
        }
        Monitor.Exit(CardsInHand);
    }

    private void RemoveCards(IEnumerable<Card> cards)
    {
        lock (CardViewMap)
        {
            List<Card> curCards = CardViewMap.Values.ToList();
            foreach (Card c in cards)
            {
                curCards.Remove(c);
            }
            ShowCards(curCards);
        }
    }

    private void ResetCardsInHand()
    {
        // clear any existing cards
        lock (CardViewMap)
        {
            foreach (CardView cv in CardViewMap.Keys)
            {
                Destroy(cv.gameObject);
            }
            CardViewMap = new Dictionary<CardView, Card>();
        }

        // create new cards
        Monitor.Enter(CardsInHand);
        Vector3 location = new Vector3(FirstCardLocation.x, FirstCardLocation.y, 0);
        foreach (Card card in CardsInHand)
        {
            CreateAndRegisterCardView(card, location);
            location.x += CardSpacing;
        }
        Monitor.PulseAll(CardsInHand);
        Monitor.Exit(CardsInHand);
    }

    private void AddKittyCards()
    {
        lock (PlayedViews)
        {
            // create new cards
            Vector3 location = new Vector3(FirstKittyCardLocation.x, FirstKittyCardLocation.y, 0);
            foreach (Card card in KittyCards)
            {
                PlayedViews.Add(CreateAndRegisterCardView(card, location));
                location.x += CardSpacing;
            }
        }
    }

    private void DestroyPlayedCard()
    {
        lock (CardViewMap)
        {
            lock (PlayedViews)
            {
                CardView cv = CardViewMap
                    .Where(kvp => PlayedCardToDestroy.Equals(kvp.Value))
                    .Select(kvp => kvp.Key)
                    .First();
                Destroy(cv.gameObject);
                CardViewMap.Remove(cv);
            }
        }
    }

    private void CheckAndCreatePlayedCards()
    {
        lock (PlayedCards)
        {
            lock (PlayedViews)
            {
                while (PlayedCards.Count > 0 && PlayedCards.Count > NumPlayedCards + 1)
                {
                    NumPlayedCards++;
                    Card card = PlayedCards[NumPlayedCards];
                    CardView cardView = Instantiate(CardPrefab, transform.position + PlayedLocations[NumPlayedCards], Quaternion.identity, transform).GetComponent<CardView>();
                    cardView.Init(card, ColorMap[card.Suit], SuitMap[card.Suit]);
                    PlayedViews.Add(cardView);
                }
            }
        }
    }

    private void ClearPlayedCards()
    {
        lock (PlayedViews)
        {
            foreach (CardView cv in PlayedViews)
            {
                Destroy(cv.gameObject);
            }
            PlayedViews = new List<CardView>();
        }

        lock (PlayedCards)
        {
            PlayedCards = new List<Card>();
            PlayedLocations = new List<Vector3>();
            NumPlayedCards = -1;
        }

        Monitor.Enter(this);
        ClearEnabled = false;
        Monitor.PulseAll(this);
        Monitor.Exit(this);
    }

    private void EnableSelect(int numSelectable, List<CardView> selectableCards)
    {
        lock (SelectableCards)
        {
            SelectableCards = selectableCards;
            DoHighlight = true;
            NumSelectable = numSelectable;
            Selectable = true;
        }
        lock (SelectedCards)
        {
            SelectedCards = new List<CardView>();
        }
    }

    private void HandleCardClicked(CardView cardView)
    {
        lock (SelectableCards)
        {
            if (Selectable && SelectableCards.Contains(cardView))
            {
                lock (SelectedCards)
                {
                    if (SelectedCards.Contains(cardView))
                    {
                        SetInteractable(false);
                        SelectedCards.Remove(cardView);
                        cardView.Select(false);
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
        }
    }

    private CardView CreateAndRegisterCardView(Card card, Vector3 location)
    {
        GameObject cardObject = Instantiate(CardPrefab, transform.position + location, Quaternion.identity, transform);
        CardView cardView = cardObject.GetComponent<CardView>();
        cardView.Init(card, ColorMap[card.Suit], SuitMap[card.Suit]);
        cardView.RegisterSelectListener(() => HandleCardClicked(cardView));
        lock (CardViewMap)
        {
            CardViewMap.Add(cardView, card);
        }
        return cardView;
    }

    private IEnumerable<Card> GetSelectedCards()
    {
        lock (CardViewMap)
        {
            lock (SelectedCards)
            {
                return CardViewMap.Where(kvp => SelectedCards.Contains(kvp.Key)).Select(kvp => kvp.Value);
            }
        }
    }

    private void HighlightSelectableCards(bool highlight)
    {
        lock (SelectableCards)
        {
            foreach (CardView cv in SelectableCards)
            {
                cv.Highlight(highlight);
            }
        }
    }

    private void SetInteractable(bool interactable)
    {
        PlayButton.interactable = interactable;
        DiscardButton.interactable = interactable;
        PassButton.interactable = interactable;
    }

    private void ResetButtonsAndSelections()
    {
        // buttons
        SetInteractable(false);
        DiscardEnabled = false;
        PlayEnabled = false;
        PassEnabled = false;

        // unhighlight cards
        HighlightSelectableCards(false);
        lock (SelectableCards)
        {
            NumSelectable = 0;
            Selectable = false;
            DoHighlight = false;
            SelectableCards = new List<CardView>();
        }
    }
}
