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

    [Header("Scene References")]
    [SerializeField] private GameObject mysteryCouponTemplate;
    [SerializeField] private Transform cardDestination;
    [SerializeField] private GameObject collectRewardButton;
    [SerializeField] private GameObject buyButton;
    [SerializeField] private TextMeshPro moneyText;

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
                else if (collectRewardButton != null && collectRewardButton.activeSelf
                         && hit.collider.gameObject == collectRewardButton)
                {
                    CollectReward();
                }
            }
        }
    }

    private void TryBuyCard()
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

        ScratchCard sc = newCard.GetComponentInChildren<ScratchCard>();
        if (sc != null)
        {
            GameObject cardRef = newCard;
            ScratchCard scRef = sc;
            RewardManager rmRef = rm;

            sc.OnScratched += (percentage) => OnCardScratched(cardRef, scRef, rmRef, percentage);
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

    private void OnCardScratched(GameObject card, ScratchCard sc, RewardManager rm, float scratchPercentage)
    {
        if (rewardRevealedCards.Contains(card)) return;

        if (scratchPercentage >= scratchThreshold)
        {
            rewardRevealedCards.Add(card);

            currentCard = card;
            currentScratchCard = sc;
            currentRewardManager = rm;
            rewardReady = true;

            Debug.Log("Card " + Mathf.FloorToInt(scratchPercentage * 100) + "% scratched! Reward revealed!");

            if (collectRewardButton != null)
            {
                collectRewardButton.SetActive(true);
            }

            int rewardValue = rm != null ? rm.ActiveRewardValue : 0;
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
        if (currentRewardManager != null)
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

        Destroy(currentCard);
        currentCard = null;
        currentScratchCard = null;
        currentRewardManager = null;
        rewardReady = false;

        if (collectRewardButton != null)
        {
            collectRewardButton.SetActive(false);
        }
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = "$" + playerMoney;
        }
    }
}
