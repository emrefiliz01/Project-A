using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // ─────────────────────────── Singleton ────────────────────────────
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Fired whenever the player's money balance changes.
    /// Passes the new balance. Subscribe in UpgradeUI, etc.
    /// </summary>
    public static event System.Action<int> OnMoneyChanged;

    // ─────────────────────────── Public money API ─────────────────────
    /// <summary>Current player balance (read-only from outside).</summary>
    public int PlayerMoney => playerMoney;

    /// <summary>Deducts <paramref name="amount"/> from the player's balance and fires OnMoneyChanged.</summary>
    public void SpendMoney(int amount)
    {
        playerMoney -= amount;
        UpdateMoneyUI();
    }

    /// <summary>Adds <paramref name="amount"/> to the player's balance and fires OnMoneyChanged.</summary>
    public void AddMoney(int amount)
    {
        playerMoney = Mathf.Max(0, playerMoney + amount);
        UpdateMoneyUI();
    }

    [Header("Player")]
    [SerializeField] private int startingMoney = 10;

    [Header("Card Settings")]
    [SerializeField] private ScratchCardData currentCardData;
    [SerializeField] private int maxCards = 20;

    [Header("Star Scratch Card Settings")]
    [SerializeField] private GameObject starScratchCardTemplate;
    [SerializeField] private GameObject buyStarCardButton;
    [SerializeField] private StarScratchCardScriptableObject starCardDataAsset;
    [SerializeField] private ScratchCardData starCardData;
    [SerializeField] private int starCardPrice = 100;

    [Header("Apple Tree Card Settings")]
    [SerializeField] private GameObject appleTreeCardTemplate;
    [SerializeField] private GameObject buyAppleTreeCardButton;
    [SerializeField] private AppleTreeCardScriptableObject appleTreeCardDataAsset;
    [SerializeField] private int appleTreeCardPrice = 500;

    [Header("Quick Cash Card Settings")] // Yeni eklendi
    [SerializeField] private GameObject quickCashCardTemplate;
    [SerializeField] private GameObject buyQuickCashCardButton;
    [SerializeField] private QuickCashCardScriptableObject quickCashCardDataAsset;
    [SerializeField] private int quickCashCardPrice = 5000;

    [Header("Scene References")]
    [SerializeField] private GameObject mysteryCouponTemplate;
    [SerializeField] private Transform cardDestination;
    [SerializeField] private GameObject collectRewardButton;
    [SerializeField] private GameObject buyButton;
    [SerializeField] private TextMeshPro moneyText;

    [Header("Price Image References (Affordability)")]
    [SerializeField] private SpriteRenderer mysteryCouponPriceImage;
    [SerializeField] private SpriteRenderer starCardPriceImage;
    [SerializeField] private SpriteRenderer appleTreeCardPriceImage;
    [SerializeField] private SpriteRenderer quickCashCardPriceImage;
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.red;

    [Header("Reward Button Text")]
    [SerializeField] private TextMeshPro rewardButtonText;

    [Header("Trash Bin Settings")]
    [SerializeField] private GameObject trashBinButton;

    public GameObject CollectRewardButton => collectRewardButton;
    public GameObject TrashBinButton => trashBinButton;
    public List<GameObject> ActiveCards => activeCards;

    [Header("Custom Cursor Settings")]
    [SerializeField] private Texture2D normalCursorTexture;
    [SerializeField] private Sprite normalCursorSprite;
    [SerializeField] private Vector2 normalCursorHotspot = Vector2.zero;

    [SerializeField] private Texture2D scratchCursorTexture;
    [SerializeField] private Sprite scratchCursorSprite;
    [SerializeField] private bool centerScratchHotspot = true;
    [SerializeField] private Vector2 scratchCursorHotspot = Vector2.zero;

    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    [Header("Scratch Threshold")]
    [SerializeField] private float scratchThreshold = 0.90f;

    [Header("Card Animation")]
    [SerializeField] private float cardMoveDuration = 0.6f;
    [SerializeField] private Ease cardMoveEase = Ease.OutBack;

    private int playerMoney;
    private List<GameObject> activeCards = new List<GameObject>();

    private GameObject currentCard;
    private ScratchCard currentScratchCard;
    private MultiZoneScratchCard currentMultiCard;
    private RewardManager currentRewardManager;
    private bool rewardReady = false;

    private HashSet<GameObject> rewardRevealedCards = new HashSet<GameObject>();

    private void Awake()
    {
        Instance = this;

        SetNormalCursor();

        playerMoney = startingMoney;

        if (mysteryCouponTemplate != null) mysteryCouponTemplate.SetActive(false);
        if (starScratchCardTemplate != null) starScratchCardTemplate.SetActive(false);
        if (appleTreeCardTemplate != null) appleTreeCardTemplate.SetActive(false);
        if (quickCashCardTemplate != null) quickCashCardTemplate.SetActive(false);
        if (collectRewardButton != null) collectRewardButton.SetActive(false);

        UpdateMoneyUI();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

            if (hit.collider != null)
            {
                if (buyButton != null && hit.collider.gameObject == buyButton)
                {
                    TryBuyCard();
                }
                else if (buyStarCardButton != null && hit.collider.gameObject == buyStarCardButton)
                {
                    TryBuyStarCard();
                }
                else if (buyAppleTreeCardButton != null && hit.collider.gameObject == buyAppleTreeCardButton)
                {
                    TryBuyAppleTreeCard();
                }
                else if (buyQuickCashCardButton != null && hit.collider.gameObject == buyQuickCashCardButton)
                {
                    TryBuyQuickCashCard();
                }
                else if (collectRewardButton != null && collectRewardButton.activeSelf
                         && hit.collider.gameObject == collectRewardButton)
                {
                    CollectReward();
                }
                else if (trashBinButton != null && hit.collider.gameObject == trashBinButton)
                {
                    DiscardCurrentCard();
                }
            }
        }
    }

    public void TryBuyCard()
    {
        if (activeCards.Count >= maxCards) return;
        int price = currentCardData != null ? currentCardData.purchasePrice : 10;
        if (playerMoney < price) return;

        playerMoney -= price;
        UpdateMoneyUI();

        GameObject newCard = Instantiate(mysteryCouponTemplate);
        newCard.SetActive(true);
        newCard.transform.position = mysteryCouponTemplate.transform.position;
        newCard.transform.rotation = mysteryCouponTemplate.transform.rotation;
        newCard.transform.localScale = mysteryCouponTemplate.transform.localScale;

        RewardManager rm = newCard.GetComponent<RewardManager>();
        if (rm != null && currentCardData != null) rm.Initialize(currentCardData);

        MultiZoneScratchCard multiCard = newCard.GetComponent<MultiZoneScratchCard>();
        if (multiCard != null)
        {
            GameObject cardRef = newCard;
            RewardManager rmRef = rm;
            MultiZoneScratchCard multiRef = multiCard;

            if (currentCardData != null && currentCardData.rewardsList != null)
                multiCard.InitializeSpotRewards(currentCardData.rewardsList);

            multiCard.OnZoneRevealedEvent += (idx, zone, r) => OnZoneProgressRevealed(cardRef, multiRef);
            multiCard.OnCardScratchedEvent += (percentage) => OnCardScratched(cardRef, null, multiRef, rmRef, percentage);
            multiCard.OnAllZonesRevealedEvent += (mCard) => OnCardScratched(cardRef, null, multiRef, rmRef, 1.0f);
        }
        else
        {
            ScratchCard sc = newCard.GetComponentInChildren<ScratchCard>();
            if (sc != null)
            {
                if (currentCardData != null)
                {
                    sc.scratchThreshold = currentCardData.scratchThreshold;
                    sc.UseLocalizedRewardCheck = currentCardData.useLocalizedRewardCheck;
                    sc.RewardSymbolBounds = currentCardData.rewardSymbolBounds;
                    sc.SymbolZoneThreshold = currentCardData.symbolZoneThreshold;
                }
                GameObject cardRef = newCard;
                ScratchCard scRef = sc;
                RewardManager rmRef = rm;
                sc.OnScratched += (percentage) => OnCardScratched(cardRef, scRef, null, rmRef, percentage);
            }
        }

        activeCards.Add(newCard);
        AnimateCardToTable(newCard);
    }

    public void TryBuyStarCard()
    {
        if (activeCards.Count >= maxCards || starScratchCardTemplate == null) return;
        int price = starCardDataAsset != null ? starCardDataAsset.purchasePrice : (starCardData != null ? starCardData.purchasePrice : starCardPrice);
        if (playerMoney < price) return;

        playerMoney -= price;
        UpdateMoneyUI();

        GameObject newCard = Instantiate(starScratchCardTemplate);
        newCard.SetActive(true);
        newCard.transform.position = starScratchCardTemplate.transform.position;
        newCard.transform.rotation = starScratchCardTemplate.transform.rotation;
        newCard.transform.localScale = starScratchCardTemplate.transform.localScale;

        MultiZoneScratchCard multiCard = newCard.GetComponent<MultiZoneScratchCard>();
        if (multiCard != null)
        {
            GameObject cardRef = newCard;
            MultiZoneScratchCard multiRef = multiCard;

            if (starCardDataAsset != null) multiCard.InitializeFromStarData(starCardDataAsset);
            else if (starCardData != null && starCardData.rewardsList != null) multiCard.InitializeSpotRewards(starCardData.rewardsList);

            multiCard.OnZoneRevealedEvent += (idx, zone, r) => OnZoneProgressRevealed(cardRef, multiRef);
            multiCard.OnCardScratchedEvent += (percentage) => OnCardScratched(cardRef, null, multiRef, null, percentage);
            multiCard.OnAllZonesRevealedEvent += (mCard) => OnCardScratched(cardRef, null, multiRef, null, 1.0f);
        }

        activeCards.Add(newCard);
        AnimateCardToTable(newCard);
    }

    public void TryBuyAppleTreeCard()
    {
        if (activeCards.Count >= maxCards || appleTreeCardTemplate == null) return;

        int price = appleTreeCardDataAsset != null ? appleTreeCardDataAsset.purchasePrice : appleTreeCardPrice;
        if (playerMoney < price) return;

        playerMoney -= price;
        UpdateMoneyUI();

        GameObject newCard = Instantiate(appleTreeCardTemplate);
        newCard.SetActive(true);
        newCard.transform.position = appleTreeCardTemplate.transform.position;
        newCard.transform.rotation = appleTreeCardTemplate.transform.rotation;
        newCard.transform.localScale = appleTreeCardTemplate.transform.localScale;

        MultiZoneScratchCard multiCard = newCard.GetComponent<MultiZoneScratchCard>();
        if (multiCard != null)
        {
            GameObject cardRef = newCard;
            MultiZoneScratchCard multiRef = multiCard;

            if (appleTreeCardDataAsset != null)
            {
                multiCard.InitializeFromAppleTreeData(appleTreeCardDataAsset);
            }

            multiCard.OnZoneRevealedEvent += (idx, zone, r) => OnZoneProgressRevealed(cardRef, multiRef);
            multiCard.OnCardScratchedEvent += (percentage) => OnCardScratched(cardRef, null, multiRef, null, percentage);
            multiCard.OnAllZonesRevealedEvent += (mCard) => OnCardScratched(cardRef, null, multiRef, null, 1.0f);
        }

        activeCards.Add(newCard);
        AnimateCardToTable(newCard);
    }

    public void TryBuyQuickCashCard()
    {
        if (activeCards.Count >= maxCards || quickCashCardTemplate == null) return;

        int price = quickCashCardDataAsset != null ? quickCashCardDataAsset.purchasePrice : quickCashCardPrice;
        if (playerMoney < price) return;

        playerMoney -= price;
        UpdateMoneyUI();

        GameObject newCard = Instantiate(quickCashCardTemplate);
        newCard.SetActive(true);
        newCard.transform.position = quickCashCardTemplate.transform.position;
        newCard.transform.rotation = quickCashCardTemplate.transform.rotation;
        newCard.transform.localScale = quickCashCardTemplate.transform.localScale;

        MultiZoneScratchCard multiCard = newCard.GetComponent<MultiZoneScratchCard>();
        if (multiCard != null)
        {
            GameObject cardRef = newCard;
            MultiZoneScratchCard multiRef = multiCard;

            if (quickCashCardDataAsset != null)
            {
                multiCard.InitializeFromQuickCashData(quickCashCardDataAsset);
            }

            multiCard.OnZoneRevealedEvent += (idx, zone, r) => OnZoneProgressRevealed(cardRef, multiRef);
            multiCard.OnCardScratchedEvent += (percentage) => OnCardScratched(cardRef, null, multiRef, null, percentage);
            multiCard.OnAllZonesRevealedEvent += (mCard) => OnCardScratched(cardRef, null, multiRef, null, 1.0f);
        }

        activeCards.Add(newCard);
        AnimateCardToTable(newCard);
    }

    private void AnimateCardToTable(GameObject newCard)
    {
        Vector3 destination = GetRandomDestinationPosition();
        newCard.transform.DOMove(destination, cardMoveDuration)
            .SetEase(cardMoveEase)
            .OnComplete(() =>
            {
                CardZoomController czc = newCard.GetComponent<CardZoomController>();
                if (czc != null)
                {
                    czc.SetHomePosition(destination, newCard.transform.rotation, newCard.transform.localScale);
                }
            });
    }

    private Vector3 GetRandomDestinationPosition()
    {
        if (cardDestination == null) return Vector3.zero;

        Collider2D col = cardDestination.GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds b = col.bounds;
            return new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
        }

        SpriteRenderer sr = cardDestination.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Bounds b = sr.bounds;
            return new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
        }

        return cardDestination.position;
    }

    private void OnZoneProgressRevealed(GameObject card, MultiZoneScratchCard multiCard)
    {
        if (card == null || multiCard == null) return;

        currentCard = card;
        currentMultiCard = multiCard;
        currentScratchCard = null;
        currentRewardManager = null;

        int rewardValue = multiCard.CalculateRevealedWinnings();
        UpdateCollectRewardUI(rewardValue, forceShow: multiCard.IsCompleted);
    }

    private void OnCardScratched(GameObject card, ScratchCard sc, MultiZoneScratchCard multiCard, RewardManager rm, float scratchPercentage)
    {
        currentCard = card;
        currentScratchCard = sc;
        currentMultiCard = multiCard;
        currentRewardManager = rm;

        float targetThreshold = scratchThreshold;
        if (multiCard != null) targetThreshold = multiCard.OverallCompletionThreshold;
        else if (sc != null) targetThreshold = sc.scratchThreshold;
        else if (currentCardData != null) targetThreshold = currentCardData.scratchThreshold;

        bool isScCompleted = sc != null && sc.IsCompleted;
        bool isMultiCompleted = multiCard != null && multiCard.IsCompleted;
        bool isFullyFinished = scratchPercentage >= targetThreshold || isScCompleted || isMultiCompleted;

        if ((multiCard != null && multiCard.HasAnyZoneRevealed) || (sc != null && isScCompleted) || (rm != null && isFullyFinished) || (scratchPercentage >= targetThreshold) || isFullyFinished)
        {
            int rewardValue = 0;
            if (multiCard != null) rewardValue = multiCard.CalculateRevealedWinnings();
            else if (rm != null) rewardValue = rm.ActiveRewardValue;

            UpdateCollectRewardUI(rewardValue, forceShow: isFullyFinished);
        }

        if (isFullyFinished)
        {
            rewardRevealedCards.Add(card);
        }
    }

    private void UpdateCollectRewardUI(int rewardValue, bool forceShow = false)
    {
        if (CardZoomController.CurrentlyZoomedCard == null)
        {
            if (collectRewardButton != null)
            {
                collectRewardButton.SetActive(false);
            }
            rewardReady = false;
            return;
        }

        bool wasAlreadyActive = collectRewardButton != null && collectRewardButton.activeSelf;
        bool shouldShow = (rewardValue != 0) || wasAlreadyActive || forceShow;

        rewardReady = shouldShow;

        if (collectRewardButton != null)
        {
            collectRewardButton.SetActive(shouldShow);
        }

        if (shouldShow && rewardButtonText != null)
        {
            if (rewardValue >= 0)
            {
                rewardButtonText.text = "+$" + rewardValue;
            }
            else
            {
                rewardButtonText.text = "-$" + Mathf.Abs(rewardValue);
            }
        }
    }

    private void CollectReward()
    {
        GameObject cardToCollect = currentCard;
        if (cardToCollect == null && CardZoomController.CurrentlyZoomedCard != null)
        {
            cardToCollect = CardZoomController.CurrentlyZoomedCard.gameObject;
        }

        if (cardToCollect == null) return;

        int rewardValue = 0;
        if (currentMultiCard != null)
        {
            rewardValue = currentMultiCard.CalculateRevealedWinnings();
        }
        else if (currentRewardManager != null)
        {
            rewardValue = currentRewardManager.ActiveRewardValue;
            currentRewardManager.ClaimReward();
        }
        else
        {
            MultiZoneScratchCard mc = cardToCollect.GetComponent<MultiZoneScratchCard>();
            RewardManager rm = cardToCollect.GetComponent<RewardManager>();
            if (mc != null) rewardValue = mc.CalculateRevealedWinnings();
            else if (rm != null) rewardValue = rm.ActiveRewardValue;
        }

        AddMoney(rewardValue);

        activeCards.Remove(cardToCollect);
        rewardRevealedCards.Remove(cardToCollect);

        ScratchCard sc = cardToCollect.GetComponentInChildren<ScratchCard>();
        if (sc != null) sc.OnScratched = null;
        if (CardInfoPanelUI.Instance != null) CardInfoPanelUI.Instance.HidePanel();

        SetNormalCursor();

        Destroy(cardToCollect);
        currentCard = null;
        currentScratchCard = null;
        currentMultiCard = null;
        currentRewardManager = null;
        rewardReady = false;

        if (collectRewardButton != null) collectRewardButton.SetActive(false);
    }

    /// <summary>
    /// Discards a specific scratch card GameObject without claiming winnings or applying penalties.
    /// Used when dragging a card to the Trash Bin or trashing an active card.
    /// </summary>
    public void DiscardCard(GameObject cardToDiscard)
    {
        if (cardToDiscard == null) return;

        activeCards.Remove(cardToDiscard);
        rewardRevealedCards.Remove(cardToDiscard);

        ScratchCard sc = cardToDiscard.GetComponentInChildren<ScratchCard>();
        if (sc != null) sc.OnScratched = null;

        MultiZoneScratchCard mc = cardToDiscard.GetComponent<MultiZoneScratchCard>();
        if (mc != null)
        {
            mc.OnZoneRevealedEvent = null;
            mc.OnCardScratchedEvent = null;
            mc.OnAllZonesRevealedEvent = null;
        }

        bool wasCurrentOrZoomed = (currentCard == cardToDiscard) ||
                                  (CardZoomController.CurrentlyZoomedCard != null && CardZoomController.CurrentlyZoomedCard.gameObject == cardToDiscard);

        if (wasCurrentOrZoomed)
        {
            if (CardInfoPanelUI.Instance != null)
                CardInfoPanelUI.Instance.HidePanel();

            if (collectRewardButton != null)
                collectRewardButton.SetActive(false);

            SetNormalCursor();

            currentCard = null;
            currentScratchCard = null;
            currentMultiCard = null;
            currentRewardManager = null;
            rewardReady = false;
        }

        Destroy(cardToDiscard);
    }

    /// <summary>
    /// Discards the currently active / zoomed scratch card without claiming winnings or applying penalties.
    /// Can be called via the Trash Bin GameObject raycast or directly by a UI Button.
    /// </summary>
    public void DiscardCurrentCard()
    {
        GameObject cardToDiscard = currentCard;
        if (cardToDiscard == null && CardZoomController.CurrentlyZoomedCard != null)
        {
            cardToDiscard = CardZoomController.CurrentlyZoomedCard.gameObject;
        }

        if (cardToDiscard != null)
        {
            DiscardCard(cardToDiscard);
        }
    }

    /// <summary>
    /// Invoked when a card is zoomed in (Scratch Mode).
    /// Restores the CollectRewardButton if this card already has a reward or zone revealed.
    /// </summary>
    public void OnCardZoomedIn(GameObject card)
    {
        if (card == null) return;
        currentCard = card;
        currentMultiCard = card.GetComponent<MultiZoneScratchCard>();
        currentScratchCard = card.GetComponentInChildren<ScratchCard>();
        currentRewardManager = card.GetComponent<RewardManager>();

        if (currentMultiCard != null)
        {
            if (currentMultiCard.HasAnyZoneRevealed || currentMultiCard.IsCompleted)
            {
                int rewardValue = currentMultiCard.CalculateRevealedWinnings();
                UpdateCollectRewardUI(rewardValue, forceShow: currentMultiCard.IsCompleted);
            }
            else
            {
                if (collectRewardButton != null) collectRewardButton.SetActive(false);
            }
        }
        else if (currentScratchCard != null || currentRewardManager != null)
        {
            bool isCardFinished = (currentScratchCard != null && currentScratchCard.IsCompleted) || rewardRevealedCards.Contains(card);
            if (isCardFinished)
            {
                int rewardValue = currentRewardManager != null ? currentRewardManager.ActiveRewardValue : 0;
                UpdateCollectRewardUI(rewardValue, forceShow: true);
            }
            else
            {
                if (collectRewardButton != null) collectRewardButton.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Invoked when zooming back out to the table view.
    /// Immediately hides the CollectRewardButton until the card is reopened.
    /// </summary>
    public void OnCardZoomedOut(GameObject card)
    {
        if (collectRewardButton != null)
        {
            collectRewardButton.SetActive(false);
        }
    }

    private void Start() => UpdateMoneyUI();

    private void UpdateMoneyUI()
    {
        if (moneyText != null) moneyText.text = "$" + playerMoney;
        UpdateAffordabilityUI();
        OnMoneyChanged?.Invoke(playerMoney);
    }

    private void UpdateAffordabilityUI()
    {
        int mysteryPrice = currentCardData != null ? currentCardData.purchasePrice : 10;
        int starPrice = starCardDataAsset != null ? starCardDataAsset.purchasePrice : (starCardData != null ? starCardData.purchasePrice : starCardPrice);
        int appleTreePrice = appleTreeCardDataAsset != null ? appleTreeCardDataAsset.purchasePrice : appleTreeCardPrice;
        int quickCashPrice = quickCashCardDataAsset != null ? quickCashCardDataAsset.purchasePrice : quickCashCardPrice;

        if (mysteryCouponPriceImage == null && buyButton != null) mysteryCouponPriceImage = GetPriceImage(buyButton);
        if (mysteryCouponPriceImage != null) mysteryCouponPriceImage.color = (playerMoney >= mysteryPrice) ? affordableColor : unaffordableColor;

        if (starCardPriceImage == null && buyStarCardButton != null) starCardPriceImage = GetPriceImage(buyStarCardButton);
        if (starCardPriceImage != null) starCardPriceImage.color = (playerMoney >= starPrice) ? affordableColor : unaffordableColor;

        if (appleTreeCardPriceImage == null && buyAppleTreeCardButton != null) appleTreeCardPriceImage = GetPriceImage(buyAppleTreeCardButton);
        if (appleTreeCardPriceImage != null) appleTreeCardPriceImage.color = (playerMoney >= appleTreePrice) ? affordableColor : unaffordableColor;

        if (quickCashCardPriceImage == null && buyQuickCashCardButton != null) quickCashCardPriceImage = GetPriceImage(buyQuickCashCardButton);
        if (quickCashCardPriceImage != null) quickCashCardPriceImage.color = (playerMoney >= quickCashPrice) ? affordableColor : unaffordableColor;
    }

    private SpriteRenderer GetPriceImage(GameObject buttonObj)
    {
        if (buttonObj == null) return null;
        Transform priceT = buttonObj.transform.Find("PriceImage");
        if (priceT == null) priceT = buttonObj.transform.Find("TextImage");

        if (priceT != null) return priceT.GetComponent<SpriteRenderer>();

        SpriteRenderer buttonSR = buttonObj.GetComponent<SpriteRenderer>();
        SpriteRenderer[] childSRs = buttonObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in childSRs)
        {
            if (sr != buttonSR) return sr;
        }
        return null;
    }

    public void SetNormalCursor()
    {
        if (VisualCursorFollower.Instance != null)
        {
            VisualCursorFollower.Instance.SetNormal();
        }

        Texture2D tex = normalCursorTexture;
        if (tex == null && normalCursorSprite != null)
        {
            tex = normalCursorSprite.texture;
        }

        if (tex != null)
        {
            Cursor.SetCursor(tex, normalCursorHotspot, cursorMode);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
        }
    }

    public void SetScratchCursor()
    {
        if (VisualCursorFollower.Instance != null)
        {
            VisualCursorFollower.Instance.SetScratch();
        }

        Texture2D tex = scratchCursorTexture;
        if (tex == null && scratchCursorSprite != null)
        {
            tex = scratchCursorSprite.texture;
        }

        if (tex != null)
        {
            Vector2 hot = centerScratchHotspot ? new Vector2(tex.width / 2f, tex.height / 2f) : scratchCursorHotspot;
            Cursor.SetCursor(tex, hot, cursorMode);
        }
    }
}