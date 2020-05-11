using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameView : MonoBehaviour
{
    // Player names
    public TextMeshProUGUI SouthName;
    public TextMeshProUGUI EastName;
    public TextMeshProUGUI WestName;
    private string PlayerName;
    private string EastPlayerName;
    private string WestPlayerName;

    // Card insantiation
    public GameObject CardPrefab;
    public Vector2 FirstCardLocation;
    public Vector2 FirstKittyCardLocation;
    public Vector2 SouthCardLocation;
    public Vector2 WestCardLocation;
    public Vector2 EastCardLocation;
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
    private bool ClearEnabled;
    private bool PlayEnabled;
    private bool DiscardEnabled;

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
        PlayerName = string.Empty;
        EastPlayerName = string.Empty;
        WestPlayerName = string.Empty;
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
        DiscardButton.onClick.AddListener(() => {
            IEnumerable<Card> discards = GetSelectedCards();
            RemoveCards(discards);
            Client.Instance.HandleDiscardKitty(discards);
            lock (PlayedViews)
            {
                PlayedViews = new List<CardView>();
            }
        });
        DiscardButton.onClick.AddListener(ResetButtonsAndSelections);
        PlayButton.onClick.AddListener(() => { Client.Instance.HandleTurn(GetSelectedCards().Single()); });
        PlayButton.onClick.AddListener(ResetButtonsAndSelections);
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
        if (ClearEnabled != ClearButton.gameObject.activeInHierarchy)
        {
            ClearButton.gameObject.SetActive(ClearEnabled);
        }

        // names
        if (PlayerName != string.Empty)
        {
            SouthName.text = PlayerName;
            PlayerName = string.Empty;
        }
        if (WestPlayerName != string.Empty)
        {
            WestName.text = WestPlayerName;
            WestPlayerName = string.Empty;
        }
        if (EastPlayerName != string.Empty)
        {
            EastName.text = EastPlayerName;
            EastPlayerName = string.Empty;
        }
    }

    public void ShowPlayerName(string name)
    { 
        PlayerName = name;
    }

    public void ShowPlayerName(bool isBefore, string otherPlayerName)
    {
        if (isBefore)
        {
            WestPlayerName = otherPlayerName;
        }
        else 
        {
            EastPlayerName = otherPlayerName;
        }
    }

    public void ShowCards(List<Card> cards)
    {
        lock (CardsInHand)
        {
            CardsInHand = cards.OrderBy(c => c).ToList();
        }
    }

    public void ShowKittyCards(IEnumerable<Card> kitty)
    {
        lock (KittyCards)
        {
            KittyCards = kitty.OrderBy(c => c).ToList();
        }
    }

    public void HandleDiscardKitty(int numCards)
    {
        WaitForKittyCreated();
        DiscardEnabled = true;
        EnableSelect(numCards, CardViewMap.Keys.ToList());
    }

    public void ShowPlayedCard(Card card, string player, bool destroyExisting)
    {
        Vector3 location = new Vector3(0, 0, 0);
        if (SouthName.text == player)
        {
            location = new Vector3(SouthCardLocation[0], SouthCardLocation[1], 0);
        }
        else if (WestName.text == player)
        {
            location = new Vector3(WestCardLocation[0], WestCardLocation[1], 0);
        }
        else if (EastName.text == player)
        {
            location = new Vector3(EastCardLocation[0], EastCardLocation[1], 0);
        }
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

    public void EnableClearKitty()
    {
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

    public void WaitForInitialization()
    {
        Monitor.Enter(this);
        while (!StartDone)
        {
            Monitor.Wait(this);
        }
        Monitor.Exit(this);
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

    private void WaitForKittyCreated()
    {
        Monitor.Enter(KittyCards);
        while (KittyCards.Count() > 0)
        {
            Monitor.Wait(KittyCards);
        }
        Monitor.Exit(KittyCards);
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
        lock (CardsInHand)
        {
            Vector3 location = new Vector3(FirstCardLocation.x, FirstCardLocation.y, 0);
            foreach (Card card in CardsInHand)
            {
                CreateAndRegisterCardView(card, location);
                location.x += CardSpacing;
            }
        }
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
                        PlayButton.interactable = false;
                        DiscardButton.interactable = false;
                        SelectedCards.Remove(cardView);
                        cardView.Select(false);
                    }
                    else if (SelectedCards.Count < NumSelectable)
                    {
                        PlayButton.interactable = false;
                        DiscardButton.interactable = false;
                        SelectedCards.Add(cardView);
                        cardView.Select(true);
                    }

                    if (SelectedCards.Count == NumSelectable)
                    {
                        PlayButton.interactable = true;
                        DiscardButton.interactable = true;
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

    private void ResetButtonsAndSelections()
    {
        // buttons
        DiscardButton.interactable = false;
        PlayButton.interactable = false;
        DiscardEnabled = false;
        PlayEnabled = false;

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
