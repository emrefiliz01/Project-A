using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
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

    [Header("Scene References")]
    [SerializeField] private GameObject mysteryCouponTemplate;
    [SerializeField] private Transform cardDestination;
    [SerializeField] private GameObject collectRewardButton;
    [SerializeField] private GameObject buyButton;
    [SerializeField] private TextMeshPro moneyText;

    [Header("Price Image References (Affordability)")]
    [SerializeField] private SpriteRenderer mysteryCouponPriceImage;
    [SerializeField] private SpriteRenderer starCardPriceImage;
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.red;

    [Header("Reward Button Text")]
    [SerializeField] private TextMeshPro rewardButtonText;

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
        playerMoney = startingMoney;

        if (mysteryCouponTemplate != null)
        {
            mysteryCouponTemplate.SetActive(false);
        }

        if (starScratchCardTemplate != null)
        {
            starScratchCardTemplate.SetActive(false);
        }

        if (collectRewardButton != null)
        {
            collectRewardButton.SetActive(false);
        }

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
                else if (collectRewardButton != null && collectRewardButton.activeSelf
                         && hit.collider.gameObject == collectRewardButton)
                {
                    CollectReward();
                }
            }
        }
    }

    public void TryBuyCard()
    {
        if (activeCards.Count >= maxCards)
        {
            Debug.Log("Card limit reached! Scratch some cards first. (" + activeCards.Count + "/" + maxCards + ")");
            return;
        }

        int price = currentCardData != null ? currentCardData.purchasePrice : 10;

        if (playerMoney < price)
        {
            Debug.Log("Not enough money! You need $" + price + " but only have $" + playerMoney);
            return;
        }

        playerMoney -= price;
        UpdateMoneyUI();
        Debug.Log("Bought card for $" + price + ". Remaining: $" + playerMoney + " | Cards: " + (activeCards.Count + 1) + "/" + maxCards);

        GameObject newCard = Instantiate(mysteryCouponTemplate);
        newCard.SetActive(true);
        newCard.transform.position = mysteryCouponTemplate.transform.position;
        newCard.transform.rotation = mysteryCouponTemplate.transform.rotation;
        newCard.transform.localScale = mysteryCouponTemplate.transform.localScale;

        RewardManager rm = newCard.GetComponent<RewardManager>();
        if (rm != null && currentCardData != null)
        {
            rm.Initialize(currentCardData);
        }

        MultiZoneScratchCard multiCard = newCard.GetComponent<MultiZoneScratchCard>();
        if (multiCard != null)
        {
            GameObject cardRef = newCard;
            RewardManager rmRef = rm;
            MultiZoneScratchCard multiRef = multiCard;

            if (currentCardData != null && currentCardData.rewardsList != null)
            {
                multiCard.InitializeSpotRewards(currentCardData.rewardsList);
            }

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
                }

                GameObject cardRef = newCard;
                ScratchCard scRef = sc;
                RewardManager rmRef = rm;

                sc.OnScratched += (percentage) => OnCardScratched(cardRef, scRef, null, rmRef, percentage);
            }
        }

        activeCards.Add(newCard);

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

    public void TryBuyStarCard()
    {
        if (activeCards.Count >= maxCards)
        {
            Debug.Log("Card limit reached! Scratch some cards first. (" + activeCards.Count + "/" + maxCards + ")");
            return;
        }

        if (starScratchCardTemplate == null)
        {
            Debug.LogWarning("GameManager: starScratchCardTemplate is not assigned!");
            return;
        }

        int price = starCardDataAsset != null ? starCardDataAsset.purchasePrice : (starCardData != null ? starCardData.purchasePrice : starCardPrice);

        if (playerMoney < price)
        {
            Debug.Log("Not enough money! You need $" + price + " but only have $" + playerMoney);
            return;
        }

        playerMoney -= price;
        UpdateMoneyUI();
        Debug.Log("Bought Star Scratch Card for $" + price + ". Remaining balance: $" + playerMoney + " | Cards: " + (activeCards.Count + 1) + "/" + maxCards);

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

            if (starCardDataAsset != null)
            {
                multiCard.InitializeFromStarData(starCardDataAsset);
            }
            else if (starCardData != null && starCardData.rewardsList != null)
            {
                multiCard.InitializeSpotRewards(starCardData.rewardsList);
            }

            multiCard.OnCardScratchedEvent += (percentage) => OnCardScratched(cardRef, null, multiRef, null, percentage);
            multiCard.OnAllZonesRevealedEvent += (mCard) => OnCardScratched(cardRef, null, multiRef, null, 1.0f);
        }

        activeCards.Add(newCard);

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
        if (cardDestination == null)
            return Vector3.zero;

        Collider2D col = cardDestination.GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds b = col.bounds;
            return new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                0f
            );
        }

        SpriteRenderer sr = cardDestination.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Bounds b = sr.bounds;
            return new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                0f
            );
        }

        return cardDestination.position;
    }

    private void OnCardScratched(GameObject card, ScratchCard sc, MultiZoneScratchCard multiCard, RewardManager rm, float scratchPercentage)
    {
        if (rewardRevealedCards.Contains(card)) return;

        float targetThreshold = scratchThreshold;
        if (multiCard != null)
        {
            targetThreshold = multiCard.OverallCompletionThreshold;
        }
        else if (sc != null)
        {
            targetThreshold = sc.scratchThreshold;
        }
        else if (currentCardData != null)
        {
            targetThreshold = currentCardData.scratchThreshold;
        }

        if (scratchPercentage >= targetThreshold || (multiCard != null && multiCard.IsCompleted))
        {
            rewardRevealedCards.Add(card);

            currentCard = card;
            currentScratchCard = sc;
            currentMultiCard = multiCard;
            currentRewardManager = rm;
            rewardReady = true;

            int rewardValue = 0;
            if (multiCard != null)
            {
                rewardValue = multiCard.TotalWinnings;
            }
            else if (rm != null)
            {
                rewardValue = rm.ActiveRewardValue;
            }

            Debug.Log("Card " + Mathf.FloorToInt(scratchPercentage * 100) + "% scratched! Total Reward: $" + rewardValue);

            if (collectRewardButton != null)
            {
                collectRewardButton.SetActive(true);
            }

            if (rewardButtonText != null)
            {
                rewardButtonText.text = "+$" + rewardValue;
            }
        }
    }

    private void CollectReward()
    {
        if (!rewardReady || currentCard == null) return;

        int rewardValue = 0;
        if (currentMultiCard != null)
        {
            rewardValue = currentMultiCard.TotalWinnings;
        }
        else if (currentRewardManager != null)
        {
            rewardValue = currentRewardManager.ActiveRewardValue;
            currentRewardManager.ClaimReward();
        }

        playerMoney += rewardValue;
        UpdateMoneyUI();
        Debug.Log("Collected $" + rewardValue + "! New balance: $" + playerMoney + " | Cards: " + (activeCards.Count - 1) + "/" + maxCards);

        activeCards.Remove(currentCard);
        rewardRevealedCards.Remove(currentCard);

        if (currentScratchCard != null)
        {
            currentScratchCard.OnScratched = null;
        }

        // Hide the info panel immediately
        if (CardInfoPanelUI.Instance != null)
        {
            CardInfoPanelUI.Instance.HidePanel();
        }

        Destroy(currentCard);
        currentCard = null;
        currentScratchCard = null;
        currentMultiCard = null;
        currentRewardManager = null;
        rewardReady = false;

        if (collectRewardButton != null)
        {
            collectRewardButton.SetActive(false);
        }
    }

    private void Start()
    {
        UpdateMoneyUI();
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = "$" + playerMoney;
        }

        UpdateAffordabilityUI();
    }

    private void UpdateAffordabilityUI()
    {
        int mysteryPrice = currentCardData != null ? currentCardData.purchasePrice : 10;
        int starPrice = starCardDataAsset != null ? starCardDataAsset.purchasePrice : (starCardData != null ? starCardData.purchasePrice : starCardPrice);

        // Mystery Coupon Price Image
        if (mysteryCouponPriceImage == null && buyButton != null)
        {
            mysteryCouponPriceImage = GetPriceImage(buyButton);
        }

        if (mysteryCouponPriceImage != null)
        {
            mysteryCouponPriceImage.color = (playerMoney >= mysteryPrice) ? affordableColor : unaffordableColor;
        }

        // Star Scratch Card Price Image
        if (starCardPriceImage == null && buyStarCardButton != null)
        {
            starCardPriceImage = GetPriceImage(buyStarCardButton);
        }

        if (starCardPriceImage != null)
        {
            starCardPriceImage.color = (playerMoney >= starPrice) ? affordableColor : unaffordableColor;
        }
    }

    private SpriteRenderer GetPriceImage(GameObject buttonObj)
    {
        if (buttonObj == null) return null;

        Transform priceT = buttonObj.transform.Find("PriceImage");
        if (priceT == null) priceT = buttonObj.transform.Find("TextImage");

        if (priceT != null)
        {
            return priceT.GetComponent<SpriteRenderer>();
        }

        SpriteRenderer buttonSR = buttonObj.GetComponent<SpriteRenderer>();
        SpriteRenderer[] childSRs = buttonObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in childSRs)
        {
            if (sr != buttonSR)
            {
                return sr;
            }
        }

        return null;
    }
}
