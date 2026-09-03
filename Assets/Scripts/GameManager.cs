using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event System.Action<int> OnMoneyChanged;

    public int PlayerMoney => playerMoney;

    public void SpendMoney(int amount)
    {
        playerMoney -= amount;
        UpdateMoneyUI();
    }

    public void AddMoney(int amount)
    {
        playerMoney = Mathf.Max(0, playerMoney + amount);
        UpdateMoneyUI();
    }

    public int UnfinishedCardCount
    {
        get
        {
            int count = 0;
            foreach (var card in activeCards)
            {
                if (card == null) continue;

                MultiZoneScratchCard mc = card.GetComponent<MultiZoneScratchCard>();
                if (mc != null)
                {
                    if (!mc.IsCompleted) count++;
                    continue;
                }

                ScratchCard sc = card.GetComponentInChildren<ScratchCard>();
                if (sc != null)
                {
                    if (!sc.IsCompleted && !rewardRevealedCards.Contains(card)) count++;
                    continue;
                }

                count++;
            }
            return count;
        }
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

    [Header("Quick Cash Card Settings")]
    [SerializeField] private GameObject quickCashCardTemplate;
    [SerializeField] private GameObject buyQuickCashCardButton;
    [SerializeField] private QuickCashCardScriptableObject quickCashCardDataAsset;
    [SerializeField] private int quickCashCardPrice = 5000;

    [Header("Lucky Cat Card Settings")]
    [SerializeField] private GameObject luckyCatCardTemplate;
    [SerializeField] private GameObject buyLuckyCatCardButton;
    [SerializeField] private LuckyCatCardScriptableObject luckyCatCardDataAsset;
    [SerializeField] private int luckyCatCardPrice = 150000;

    [Header("Scene References")]
    [SerializeField] private GameObject mysteryCouponTemplate;
    [SerializeField] private Transform cardDestination;
    [SerializeField] private GameObject collectRewardButton;
    [SerializeField] private GameObject buyButton;
    [SerializeField] private TextMeshPro moneyText;

    // ──────────────────────────────────────────────────────────────────────────
    // Ticket Panel UI
    // ──────────────────────────────────────────────────────────────────────────
    [Header("Ticket Panel – Price Texts")]
    [SerializeField] private TMP_Text mysteryCouponPriceText;
    [SerializeField] private TMP_Text starScratchPriceText;
    [SerializeField] private TMP_Text appleTreePriceText;
    [SerializeField] private TMP_Text quickCashPriceText;
    [SerializeField] private TMP_Text luckyCatPriceText;

    [Header("Ticket Panel – Level Texts")]
    [SerializeField] private TMP_Text mysteryCouponLevelText;
    [SerializeField] private TMP_Text starScratchLevelText;
    [SerializeField] private TMP_Text appleTreeLevelText;
    [SerializeField] private TMP_Text quickCashLevelText;
    [SerializeField] private TMP_Text luckyCatLevelText;

    [Header("Ticket Panel – EXP Bar Fills")]
    [SerializeField] private Image mysteryCouponExpBarFill;
    [SerializeField] private Image starScratchExpBarFill;
    [SerializeField] private Image appleTreeExpBarFill;
    [SerializeField] private Image quickCashExpBarFill;
    [SerializeField] private Image luckyCatExpBarFill;

    [Header("Ticket Panel – Affordability Colors")]
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
    [SerializeField] private float cardMoveDuration = 0.35f;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private int playerMoney;
    private List<GameObject> activeCards = new List<GameObject>();

    // Card Level & EXP data – one instance per card type
    private CardLevelData mysteryCouponLevel = new CardLevelData();
    private CardLevelData starScratchLevel = new CardLevelData();
    private CardLevelData appleTreeLevel = new CardLevelData();
    private CardLevelData quickCashLevel = new CardLevelData();
    private CardLevelData luckyCatLevel = new CardLevelData();

    private GameObject currentCard;
    private ScratchCard currentScratchCard;
    private MultiZoneScratchCard currentMultiCard;
    private RewardManager currentRewardManager;
    private bool rewardReady = false;

    private HashSet<GameObject> rewardRevealedCards = new HashSet<GameObject>();
    private List<GameObject> completedCardsSnapshot = new List<GameObject>();

    // ─────────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;

        SetNormalCursor();

        playerMoney = startingMoney;

        if (mysteryCouponTemplate != null) mysteryCouponTemplate.SetActive(false);
        if (starScratchCardTemplate != null) starScratchCardTemplate.SetActive(false);
        if (appleTreeCardTemplate != null) appleTreeCardTemplate.SetActive(false);
        if (quickCashCardTemplate != null) quickCashCardTemplate.SetActive(false);
        if (luckyCatCardTemplate != null) luckyCatCardTemplate.SetActive(false);
        if (collectRewardButton != null) collectRewardButton.SetActive(false);

        UpdateMoneyUI();
        UpdateAllCardLevelUI();
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
                else if (buyLuckyCatCardButton != null && hit.collider.gameObject == buyLuckyCatCardButton)
                {
                    TryBuyLuckyCatCard();
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

    // ─────────────────────────────────────────────────────────────────────────
    // Card Purchase Methods
    // ─────────────────────────────────────────────────────────────────────────

    public void TryBuyCard()
    {
        if (UnfinishedCardCount >= maxCards) return;
        int price = currentCardData != null ? currentCardData.purchasePrice : 10;
        if (playerMoney < price) return;

        playerMoney -= price;
        UpdateMoneyUI();

        GameObject newCard = Instantiate(mysteryCouponTemplate);
        newCard.SetActive(true);

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
        if (UnfinishedCardCount >= maxCards || starScratchCardTemplate == null) return;
        int price = starCardDataAsset != null ? starCardDataAsset.purchasePrice : (starCardData != null ? starCardData.purchasePrice : starCardPrice);
        if (playerMoney < price) return;

        playerMoney -= price;
        UpdateMoneyUI();

        GameObject newCard = Instantiate(starScratchCardTemplate);
        newCard.SetActive(true);

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
        if (UnfinishedCardCount >= maxCards || appleTreeCardTemplate == null) return;

        int price = appleTreeCardDataAsset != null ? appleTreeCardDataAsset.purchasePrice : appleTreeCardPrice;
        if (playerMoney < price) return;

        playerMoney -= price;
        UpdateMoneyUI();

        GameObject newCard = Instantiate(appleTreeCardTemplate);
        newCard.SetActive(true);

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
        if (UnfinishedCardCount >= maxCards || quickCashCardTemplate == null) return;

        int price = quickCashCardDataAsset != null ? quickCashCardDataAsset.purchasePrice : quickCashCardPrice;
        if (playerMoney < price) return;

        playerMoney -= price;
        UpdateMoneyUI();

        GameObject newCard = Instantiate(quickCashCardTemplate);
        newCard.SetActive(true);

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

    public void TryBuyLuckyCatCard()
    {
        if (UnfinishedCardCount >= maxCards || luckyCatCardTemplate == null) return;

        int price = luckyCatCardDataAsset != null ? luckyCatCardDataAsset.purchasePrice : luckyCatCardPrice;
        if (playerMoney < price) return;

        playerMoney -= price;
        UpdateMoneyUI();

        GameObject newCard = Instantiate(luckyCatCardTemplate);
        newCard.SetActive(true);

        MultiZoneScratchCard multiCard = newCard.GetComponent<MultiZoneScratchCard>();
        if (multiCard != null)
        {
            GameObject cardRef = newCard;
            MultiZoneScratchCard multiRef = multiCard;

            if (luckyCatCardDataAsset != null)
            {
                multiCard.InitializeFromLuckyCatData(luckyCatCardDataAsset);
            }

            multiCard.OnZoneRevealedEvent += (idx, zone, r) => OnZoneProgressRevealed(cardRef, multiRef);
            multiCard.OnCardScratchedEvent += (percentage) => OnCardScratched(cardRef, null, multiRef, null, percentage);
            multiCard.OnAllZonesRevealedEvent += (mCard) => OnCardScratched(cardRef, null, multiRef, null, 1.0f);
        }

        activeCards.Add(newCard);
        AnimateCardToTable(newCard);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Card Animation
    // ─────────────────────────────────────────────────────────────────────────

    private void AnimateCardToTable(GameObject newCard)
    {
        if (newCard == null) return;

        newCard.transform.DOKill();
        Vector3 destination = GetRandomDestinationPosition();
        Vector3 targetScale = newCard.transform.localScale;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, Random.Range(-3f, 3f));

        newCard.transform.DORotateQuaternion(targetRot, cardMoveDuration);
        newCard.transform.DOJump(destination, 0.4f, 1, cardMoveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                CardZoomController czc = newCard.GetComponent<CardZoomController>();
                if (czc != null)
                {
                    czc.SetHomePosition(destination, targetRot, targetScale);
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

    // ─────────────────────────────────────────────────────────────────────────
    // Scratch / Zone Event Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnZoneProgressRevealed(GameObject card, MultiZoneScratchCard multiCard)
    {
        if (card == null || multiCard == null) return;

        if (CardZoomController.CurrentlyZoomedCard != null && CardZoomController.CurrentlyZoomedCard.gameObject == card)
        {
            currentCard = card;
            currentMultiCard = multiCard;
            currentScratchCard = null;
            currentRewardManager = null;

            int rawRewardValue = multiCard.CalculateRevealedWinnings();
            int scaledRewardValue = GetScaledRewardForCard(card, rawRewardValue);
            UpdateCollectRewardUI(scaledRewardValue, forceShow: multiCard.IsCompleted);
        }
    }

    private void OnCardScratched(GameObject card, ScratchCard sc, MultiZoneScratchCard multiCard, RewardManager rm, float scratchPercentage)
    {
        float targetThreshold = scratchThreshold;
        if (multiCard != null) targetThreshold = multiCard.OverallCompletionThreshold;
        else if (sc != null) targetThreshold = sc.scratchThreshold;
        else if (currentCardData != null) targetThreshold = currentCardData.scratchThreshold;

        bool isScCompleted = sc != null && sc.IsCompleted;
        bool isMultiCompleted = multiCard != null && multiCard.IsCompleted;
        bool isFullyFinished = scratchPercentage >= targetThreshold || isScCompleted || isMultiCompleted;

        if (isFullyFinished)
        {
            rewardRevealedCards.Add(card);
        }

        if (CardZoomController.CurrentlyZoomedCard != null && CardZoomController.CurrentlyZoomedCard.gameObject == card)
        {
            currentCard = card;
            currentScratchCard = sc;
            currentMultiCard = multiCard;
            currentRewardManager = rm;

            if ((multiCard != null && multiCard.HasAnyZoneRevealed) || (sc != null && isScCompleted) || (rm != null && isFullyFinished) || (scratchPercentage >= targetThreshold) || isFullyFinished)
            {
                int rawRewardValue = 0;
                if (multiCard != null) rawRewardValue = multiCard.CalculateRevealedWinnings();
                else if (rm != null) rawRewardValue = rm.ActiveRewardValue;

                int scaledRewardValue = GetScaledRewardForCard(card, rawRewardValue);
                UpdateCollectRewardUI(scaledRewardValue, forceShow: isFullyFinished);
            }
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
                rewardButtonText.text = "+" + CurrencyFormatter.FormatMoney(rewardValue);
            else
                rewardButtonText.text = CurrencyFormatter.FormatMoney(rewardValue);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Card Completion Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bir kartın kazınmış/ödülü hazır halde olup olmadığını kontrol eder.
    /// </summary>
    private bool IsCardCompleted(GameObject card)
    {
        if (card == null) return false;

        MultiZoneScratchCard mc = card.GetComponent<MultiZoneScratchCard>();
        if (mc != null && mc.IsCompleted) return true;

        ScratchCard sc = card.GetComponentInChildren<ScratchCard>();
        if (sc != null && sc.IsCompleted) return true;

        if (rewardRevealedCards.Contains(card)) return true;

        return false;
    }

    /// <summary>
    /// İlk karta tıklandığı an masada bulunan tüm kazınmış kartların anlık fotoğrafını (Snapshot) alır.
    /// </summary>
    private void BuildCompletedCardsSnapshot(GameObject initialCard)
    {
        completedCardsSnapshot.Clear();

        foreach (var card in activeCards)
        {
            if (card != null && IsCardCompleted(card))
            {
                completedCardsSnapshot.Add(card);
            }
        }

        if (initialCard != null && completedCardsSnapshot.Contains(initialCard))
        {
            completedCardsSnapshot.Remove(initialCard);
            completedCardsSnapshot.Insert(0, initialCard);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Collect Reward
    // ─────────────────────────────────────────────────────────────────────────

    private void CollectReward()
    {
        GameObject cardToCollect = currentCard;
        if (cardToCollect == null && CardZoomController.CurrentlyZoomedCard != null)
        {
            cardToCollect = CardZoomController.CurrentlyZoomedCard.gameObject;
        }

        if (cardToCollect == null) return;

        // ── 1. Calculate base reward value ────────────────────────────────────
        int rawRewardValue = 0;
        if (currentMultiCard != null)
        {
            rawRewardValue = currentMultiCard.CalculateRevealedWinnings();
        }
        else if (currentRewardManager != null)
        {
            rawRewardValue = currentRewardManager.ActiveRewardValue;
            currentRewardManager.ClaimReward();
        }
        else
        {
            MultiZoneScratchCard mc = cardToCollect.GetComponent<MultiZoneScratchCard>();
            RewardManager rm = cardToCollect.GetComponent<RewardManager>();
            if (mc != null) rawRewardValue = mc.CalculateRevealedWinnings();
            else if (rm != null) rawRewardValue = rm.ActiveRewardValue;
        }

        // ── 2. Scale reward by card level, then add to player wallet ──────────
        CardLevelData levelData = GetLevelDataForCard(cardToCollect);
        int rewardValue = levelData != null ? levelData.ScaleReward(rawRewardValue) : rawRewardValue;
        AddMoney(rewardValue);

        // ── 3. Grant +1 EXP for the collected card type and refresh the UI ────
        if (levelData != null)
        {
            levelData.AddEXP();
            UpdateCardLevelUIForCard(cardToCollect);
        }

        // ── 4. Remove collected card from all tracking lists ──────────────────
        activeCards.Remove(cardToCollect);
        rewardRevealedCards.Remove(cardToCollect);
        completedCardsSnapshot.Remove(cardToCollect);

        ScratchCard sc = cardToCollect.GetComponentInChildren<ScratchCard>();
        if (sc != null) sc.OnScratched = null;

        GameObject cardToDestroy = cardToCollect;
        currentCard = null;
        currentScratchCard = null;
        currentMultiCard = null;
        currentRewardManager = null;

        // ── 5. Find next completed card in snapshot ───────────────────────────
        GameObject nextCard = null;
        while (completedCardsSnapshot.Count > 0)
        {
            GameObject candidate = completedCardsSnapshot[0];
            if (candidate != null && candidate != cardToDestroy && activeCards.Contains(candidate))
            {
                nextCard = candidate;
                break;
            }
            else
            {
                completedCardsSnapshot.RemoveAt(0);
            }
        }

        // ── 6. Destroy the collected card ─────────────────────────────────────
        Destroy(cardToDestroy);

        if (nextCard != null)
        {
            // SIĞRADAKİ KAZINMIŞ KARTA ANINDA GEÇİŞ YAP
            CardZoomController nextCzc = nextCard.GetComponent<CardZoomController>();
            if (nextCzc != null)
                nextCzc.FocusForCollection();
            else
                OnCardZoomedIn(nextCard);
        }
        else
        {
            // Anlık listedeki tüm kartlar bitti: Zoom'dan çık
            completedCardsSnapshot.Clear();

            if (CardZoomController.CurrentlyZoomedCard != null)
                CardZoomController.CurrentlyZoomedCard.ForceUnzoom();

            if (CardInfoPanelUI.Instance != null) CardInfoPanelUI.Instance.HidePanel();
            SetNormalCursor();
            rewardReady = false;
            if (collectRewardButton != null) collectRewardButton.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Discard Card (NO EXP granted here)
    // ─────────────────────────────────────────────────────────────────────────

    public void DiscardCard(GameObject cardToDiscard)
    {
        if (cardToDiscard == null) return;

        activeCards.Remove(cardToDiscard);
        rewardRevealedCards.Remove(cardToDiscard);
        completedCardsSnapshot.Remove(cardToDiscard);

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
                                  (CardZoomController.CurrentlyZoomedCard != null &&
                                   CardZoomController.CurrentlyZoomedCard.gameObject == cardToDiscard);

        GameObject nextCard = null;
        if (wasCurrentOrZoomed && completedCardsSnapshot.Count > 0)
        {
            while (completedCardsSnapshot.Count > 0)
            {
                GameObject candidate = completedCardsSnapshot[0];
                if (candidate != null && candidate != cardToDiscard && activeCards.Contains(candidate))
                {
                    nextCard = candidate;
                    break;
                }
                else
                {
                    completedCardsSnapshot.RemoveAt(0);
                }
            }
        }

        Destroy(cardToDiscard);

        if (wasCurrentOrZoomed)
        {
            currentCard = null;
            currentScratchCard = null;
            currentMultiCard = null;
            currentRewardManager = null;
            rewardReady = false;

            if (nextCard != null)
            {
                CardZoomController nextCzc = nextCard.GetComponent<CardZoomController>();
                if (nextCzc != null)
                    nextCzc.FocusForCollection();
                else
                    OnCardZoomedIn(nextCard);
            }
            else
            {
                completedCardsSnapshot.Clear();

                if (CardZoomController.CurrentlyZoomedCard != null)
                    CardZoomController.CurrentlyZoomedCard.ForceUnzoom();

                if (CardInfoPanelUI.Instance != null)
                    CardInfoPanelUI.Instance.HidePanel();

                if (collectRewardButton != null)
                    collectRewardButton.SetActive(false);

                SetNormalCursor();
            }
        }
    }

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

    // ─────────────────────────────────────────────────────────────────────────
    // Card Zoom Callbacks
    // ─────────────────────────────────────────────────────────────────────────

    public void OnCardZoomedIn(GameObject card)
    {
        if (card == null) return;

        currentCard = card;
        currentMultiCard = card.GetComponent<MultiZoneScratchCard>();
        currentScratchCard = card.GetComponentInChildren<ScratchCard>();
        currentRewardManager = card.GetComponent<RewardManager>();

        // Eğer yeni bir zoom açılıyorsa ve snapshot boşsa, anlık fotoğrafı al
        if (IsCardCompleted(card) && completedCardsSnapshot.Count == 0)
        {
            BuildCompletedCardsSnapshot(card);
        }

        if (currentMultiCard != null)
        {
            if (currentMultiCard.HasAnyZoneRevealed || currentMultiCard.IsCompleted)
            {
                int rawRewardValue = currentMultiCard.CalculateRevealedWinnings();
                int scaledRewardValue = GetScaledRewardForCard(card, rawRewardValue);
                UpdateCollectRewardUI(scaledRewardValue, forceShow: currentMultiCard.IsCompleted);
            }
            else
            {
                if (collectRewardButton != null) collectRewardButton.SetActive(false);
            }
        }
        else if (currentScratchCard != null || currentRewardManager != null)
        {
            bool isCardFinished = (currentScratchCard != null && currentScratchCard.IsCompleted) ||
                                  rewardRevealedCards.Contains(card);
            if (isCardFinished)
            {
                int rawRewardValue = currentRewardManager != null ? currentRewardManager.ActiveRewardValue : 0;
                int scaledRewardValue = GetScaledRewardForCard(card, rawRewardValue);
                UpdateCollectRewardUI(scaledRewardValue, forceShow: true);
            }
            else
            {
                if (collectRewardButton != null) collectRewardButton.SetActive(false);
            }
        }
    }

    public void OnCardZoomedOut(GameObject card)
    {
        if (CardZoomController.CurrentlyZoomedCard == null || CardZoomController.CurrentlyZoomedCard.gameObject == card)
        {
            completedCardsSnapshot.Clear();
            if (collectRewardButton != null)
            {
                collectRewardButton.SetActive(false);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Money & Affordability UI
    // ─────────────────────────────────────────────────────────────────────────

    private void Start() => UpdateMoneyUI();

    private void UpdateMoneyUI()
    {
        if (moneyText != null) moneyText.text = CurrencyFormatter.FormatMoney(playerMoney);
        UpdateAffordabilityUI();
        OnMoneyChanged?.Invoke(playerMoney);
    }

    private void UpdateAffordabilityUI()
    {
        int mysteryPrice = currentCardData != null ? currentCardData.purchasePrice : 10;
        int starPrice = starCardDataAsset != null ? starCardDataAsset.purchasePrice : (starCardData != null ? starCardData.purchasePrice : starCardPrice);
        int appleTreePrice = appleTreeCardDataAsset != null ? appleTreeCardDataAsset.purchasePrice : appleTreeCardPrice;
        int quickCashPrice = quickCashCardDataAsset != null ? quickCashCardDataAsset.purchasePrice : quickCashCardPrice;
        int luckyCatPrice = luckyCatCardDataAsset != null ? luckyCatCardDataAsset.purchasePrice : luckyCatCardPrice;

        SetPriceTextColor(mysteryCouponPriceText, playerMoney >= mysteryPrice);
        SetPriceTextColor(starScratchPriceText, playerMoney >= starPrice);
        SetPriceTextColor(appleTreePriceText, playerMoney >= appleTreePrice);
        SetPriceTextColor(quickCashPriceText, playerMoney >= quickCashPrice);
        SetPriceTextColor(luckyCatPriceText, playerMoney >= luckyCatPrice);
    }

    /// <summary>Sets a price TMP_Text to affordableColor or unaffordableColor.</summary>
    private void SetPriceTextColor(TMP_Text text, bool affordable)
    {
        if (text == null) return;
        text.color = affordable ? affordableColor : unaffordableColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Card Level & EXP UI
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Refreshes the level text and EXP bar fill for all five card types.</summary>
    private void UpdateAllCardLevelUI()
    {
        UpdateCardLevelUI(mysteryCouponLevel, mysteryCouponLevelText, mysteryCouponExpBarFill);
        UpdateCardLevelUI(starScratchLevel, starScratchLevelText, starScratchExpBarFill);
        UpdateCardLevelUI(appleTreeLevel, appleTreeLevelText, appleTreeExpBarFill);
        UpdateCardLevelUI(quickCashLevel, quickCashLevelText, quickCashExpBarFill);
        UpdateCardLevelUI(luckyCatLevel, luckyCatLevelText, luckyCatExpBarFill);
    }

    /// <summary>Updates a single card type's level text and EXP bar fill.</summary>
    private void UpdateCardLevelUI(CardLevelData data, TMP_Text levelText, Image expBarFill)
    {
        if (data == null) return;

        if (levelText != null)
            levelText.text = data.IsMaxLevel ? "Lvl 10 (MAX)" : $"Lvl {data.Level}";

        if (expBarFill != null)
            expBarFill.fillAmount = data.IsMaxLevel
                ? 1f
                : (data.RequiredEXP > 0 ? (float)data.CurrentEXP / data.RequiredEXP : 0f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Card Level Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the CardLevelData instance that corresponds to the given card GameObject.
    /// Detects card type by inspecting which ScriptableObject reference is set
    /// on the card's MultiZoneScratchCard component.
    /// Falls back to mysteryCouponLevel for Mystery Coupon (RewardManager-based) cards.
    /// Called by CardInfoPanelUI to display level-scaled reward values.
    /// </summary>
    public CardLevelData GetLevelDataForCard(GameObject card)
    {
        if (card == null) return null;

        MultiZoneScratchCard mc = card.GetComponent<MultiZoneScratchCard>();
        if (mc != null)
        {
            if (mc.QuickCashCardData != null) return quickCashLevel;
            if (mc.AppleTreeCardData != null) return appleTreeLevel;
            if (mc.StarCardData != null) return starScratchLevel;
            if (mc.LuckyCatCardData != null) return luckyCatLevel;
        }

        // Mystery Coupon – uses RewardManager, not MultiZoneScratchCard
        return mysteryCouponLevel;
    }

    /// <summary>
    /// Scales a card's raw reward value based on its current level.
    /// </summary>
    public int GetScaledRewardForCard(GameObject card, int baseReward)
    {
        if (card == null) return baseReward;
        CardLevelData levelData = GetLevelDataForCard(card);
        return levelData != null ? levelData.ScaleReward(baseReward) : baseReward;
    }

    /// <summary>
    /// Refreshes only the Level/EXP UI panel for the card type that matches the given card.
    /// </summary>
    private void UpdateCardLevelUIForCard(GameObject card)
    {
        if (card == null) return;

        MultiZoneScratchCard mc = card.GetComponent<MultiZoneScratchCard>();
        if (mc != null)
        {
            if (mc.QuickCashCardData != null) { UpdateCardLevelUI(quickCashLevel, quickCashLevelText, quickCashExpBarFill); return; }
            if (mc.AppleTreeCardData != null) { UpdateCardLevelUI(appleTreeLevel, appleTreeLevelText, appleTreeExpBarFill); return; }
            if (mc.StarCardData != null) { UpdateCardLevelUI(starScratchLevel, starScratchLevelText, starScratchExpBarFill); return; }
            if (mc.LuckyCatCardData != null) { UpdateCardLevelUI(luckyCatLevel, luckyCatLevelText, luckyCatExpBarFill); return; }
        }

        // Mystery Coupon fallback
        UpdateCardLevelUI(mysteryCouponLevel, mysteryCouponLevelText, mysteryCouponExpBarFill);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cursor
    // ─────────────────────────────────────────────────────────────────────────

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
            Cursor.SetCursor(tex, normalCursorHotspot, cursorMode);
        else
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
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