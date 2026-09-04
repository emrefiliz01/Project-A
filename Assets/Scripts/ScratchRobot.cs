using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public class ScratchRobot : MonoBehaviour
{
    public static ScratchRobot Instance { get; private set; }
    public static event System.Action<GameObject> OnCardProcessed;

    [Header("Capacity & Queue Settings")]
    [SerializeField] private int maxCapacity = 4;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private Transform cardStackSpot;       // Robotun kafasının üstündeki istif noktası
    [SerializeField] private Vector3 stackOffset = new Vector3(0.03f, 0.08f, 0f); // Üst üste dizilme kayma miktarı

    [Header("Robot Settings")]
    [SerializeField] private float processDuration = 8f;
    [SerializeField] private Transform cardHoldingSpot;      // Ağız/Giriş
    [SerializeField] private Transform cardDestinationSpot;  // İç/Kayma noktası
    [SerializeField] private Transform cardThrowSpot;        // Masaya fırlatma

    public Transform IntakeSpot => cardHoldingSpot != null ? cardHoldingSpot : transform;

    [Header("Throw Scatter Settings")]
    [Tooltip("Kartlar fırlatılırken masaya ne kadar dağınık saçılacak? (X ve Y ekseni varyasyonu)")]
    [SerializeField] private Vector2 throwScatterArea = new Vector2(0.6f, 0.4f);
    [Tooltip("Kartlar masaya düşerken yapacağı maksimum rastgele dönme açısı")]
    [SerializeField] private float throwMaxRotationAngle = 15f;

    [Header("Animation Timings & FX")]
    [SerializeField] private float moveInDuration = 0.25f;
    [SerializeField] private float throwDuration = 0.35f;
    [SerializeField] private float jumpPower = 0.8f;
    [SerializeField] private float shakeStrength = 0.12f;

    [Range(0.1f, 1.0f)]
    [SerializeField] private float processingScaleFactor = 0.7f;

    [Header("Ease Settings")]
    [SerializeField] private Ease moveInEase = Ease.InQuad;
    [SerializeField] private Ease throwEase = Ease.OutQuad;

    private bool isProcessing = false;
    private List<GameObject> cardQueueList = new List<GameObject>();
    private Dictionary<GameObject, Vector3> cardOriginalScales = new Dictionary<GameObject, Vector3>();
    private GameObject currentProcessingCard;
    private SpriteRenderer robotSpriteRenderer;

    public bool IsFull => TotalCardCount >= maxCapacity;
    public int TotalCardCount => (currentProcessingCard != null ? 1 : 0) + cardQueueList.Count;

    private void Awake()
    {
        Instance = this;
        robotSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        UpdateCapacityUI();
    }

    public bool AcceptCard(GameObject cardObj)
    {
        // 1. Robot doluysa, kart yoksa, zaten robottaysa VEYA kart zaten kazınmışsa KABUL ETME!
        if (IsFull || cardObj == null || IsCardScratched(cardObj) || IsCardInRobot(cardObj)) return false;

        SetCardInteractions(cardObj, false);

        if (!cardOriginalScales.ContainsKey(cardObj))
        {
            cardOriginalScales[cardObj] = cardObj.transform.localScale;
        }

        cardQueueList.Add(cardObj);
        UpdateCapacityUI();
        AnimateCardToStack(cardObj, cardQueueList.Count - 1);

        if (!isProcessing)
        {
            StartCoroutine(ProcessQueueRoutine());
        }

        return true;
    }

    /// <summary>
    /// Kartın şu anda robotun içinde işlendiğini veya robotun kuyruk istifinde olduğunu kontrol eder.
    /// </summary>
    public bool IsCardInRobot(GameObject cardObj)
    {
        if (cardObj == null) return false;
        return currentProcessingCard == cardObj || cardQueueList.Contains(cardObj);
    }

    /// <summary>
    /// Kartın daha önce kazınıp kazınmadığını veya tamamlanıp tamamlanmadığını kontrol eder.
    /// </summary>
    public static bool IsCardScratched(GameObject cardObj)
    {
        if (cardObj == null) return false;

        // MultiZone kart kontrolü (Tamamlanmış mı veya herhangı bir alanı açılmış mı?)
        MultiZoneScratchCard mc = cardObj.GetComponent<MultiZoneScratchCard>();
        if (mc != null)
        {
            if (mc.IsCompleted || mc.HasAnyZoneRevealed) return true;
        }

        // Standart ScratchCard kontrolü
        ScratchCard sc = cardObj.GetComponentInChildren<ScratchCard>();
        if (sc != null)
        {
            if (sc.IsCompleted || sc.GetScratchedPercentage() > 0.01f) return true;
        }

        return false;
    }

    private void AnimateCardToStack(GameObject cardObj, int stackIndex)
    {
        if (cardObj == null) return;

        Vector3 basePos = cardStackSpot != null ? cardStackSpot.position : transform.position + new Vector3(0f, 1.5f, 0f);
        Vector3 targetPos = basePos + (stackOffset * stackIndex);

        Vector3 baseScale = cardOriginalScales.TryGetValue(cardObj, out Vector3 storedScale) ? storedScale : cardObj.transform.localScale;
        Vector3 targetScale = baseScale * processingScaleFactor;

        Quaternion randomRot = Quaternion.Euler(0f, 0f, Random.Range(-4f, 4f));

        int robotOrder = robotSpriteRenderer != null ? robotSpriteRenderer.sortingOrder : 10;
        
        SetCardGroupSortingOrder(cardObj, robotOrder - 10 + stackIndex);

        cardObj.transform.DOKill();
        cardObj.transform.DOMove(targetPos, 0.3f).SetEase(Ease.OutQuad);
        cardObj.transform.DOScale(targetScale, 0.3f);
        cardObj.transform.DORotateQuaternion(randomRot, 0.3f);
    }

    private void RearrangeStackVisuals()
    {
        for (int i = 0; i < cardQueueList.Count; i++)
        {
            if (cardQueueList[i] != null)
            {
                AnimateCardToStack(cardQueueList[i], i);
            }
        }
    }

    private IEnumerator ProcessQueueRoutine()
    {
        isProcessing = true;

        while (cardQueueList.Count > 0)
        {
            currentProcessingCard = cardQueueList[0];
            cardQueueList.RemoveAt(0);

            RearrangeStackVisuals();

            yield return StartCoroutine(ProcessSingleCardRoutine(currentProcessingCard));

            currentProcessingCard = null;
            UpdateCapacityUI();
        }

        isProcessing = false;
        UpdateCapacityUI();
    }

    private IEnumerator ProcessSingleCardRoutine(GameObject cardObj)
    {
        if (cardObj == null) yield break;

        Vector3 originalScale = cardOriginalScales.TryGetValue(cardObj, out Vector3 storedScale) ? storedScale : cardObj.transform.localScale;
        Vector3 targetProcessingScale = originalScale * processingScaleFactor;

        Vector3 holdPos = cardHoldingSpot != null ? cardHoldingSpot.position : transform.position;
        Vector3 destPos = cardDestinationSpot != null ? cardDestinationSpot.position : holdPos + new Vector3(0f, -1.2f, 0f);

        // 1. Ağza çekilme
        cardObj.transform.DOMove(holdPos, moveInDuration).SetEase(moveInEase);
        cardObj.transform.DOScale(targetProcessingScale, moveInDuration);
        cardObj.transform.DORotate(Vector3.zero, moveInDuration);

        yield return new WaitForSeconds(moveInDuration);
        if (cardObj == null) yield break;

        // 2. Robotun içine girme katmanı
        int robotOrder = robotSpriteRenderer != null ? robotSpriteRenderer.sortingOrder : 10;
        SetCardGroupSortingOrder(cardObj, robotOrder - 1);

        // 3. İçeride aşağı kayma ve sallanma
        transform.DOShakePosition(processDuration, shakeStrength, 25);
        cardObj.transform.DOMove(destPos, processDuration).SetEase(Ease.Linear);

        yield return new WaitForSeconds(processDuration);
        if (cardObj == null) yield break;

        // 4. Tüm alanları aç
        MultiZoneScratchCard multiCard = cardObj.GetComponent<MultiZoneScratchCard>();
        if (multiCard != null)
        {
            multiCard.RevealAllZones();
        }
        else
        {
            ScratchCard[] scratchCards = cardObj.GetComponentsInChildren<ScratchCard>(true);
            foreach (var sc in scratchCards)
            {
                if (sc != null) sc.ClearAll();
            }
        }

        // 5. Masaya fırlatma
        SetCardGroupSortingOrder(cardObj, robotOrder + 2);

        Vector3 baseThrowPos = cardThrowSpot != null ? cardThrowSpot.position : transform.position + new Vector3(2.5f, -1.5f, 0f);
        
        float randomX = Random.Range(-throwScatterArea.x / 2f, throwScatterArea.x / 2f);
        float randomY = Random.Range(-throwScatterArea.y / 2f, throwScatterArea.y / 2f);
        Vector3 finalThrowPos = baseThrowPos + new Vector3(randomX, randomY, 0f);

        Quaternion finalThrowRotation = Quaternion.Euler(0f, 0f, Random.Range(-throwMaxRotationAngle, throwMaxRotationAngle));

        cardObj.transform.DOScale(originalScale, throwDuration);
        cardObj.transform.DORotateQuaternion(finalThrowRotation, throwDuration);

        cardOriginalScales.Remove(cardObj);

        bool throwFinished = false;
        cardObj.transform.DOJump(finalThrowPos, jumpPower, 1, throwDuration)
            .SetEase(throwEase)
            .OnComplete(() =>
            {
                if (cardObj != null)
                {
                    SetCardInteractions(cardObj, true);

                    CardZoomController czc = cardObj.GetComponent<CardZoomController>();
                    if (czc != null)
                    {
                        czc.SetHomePosition(finalThrowPos, finalThrowRotation, originalScale);
                    }

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnCardZoomedOut(cardObj);
                    }

                    OnCardProcessed?.Invoke(cardObj);
                }

                throwFinished = true;
            });

        while (!throwFinished) yield return null;
    }

    private void UpdateCapacityUI()
    {
        if (capacityText != null)
        {
            capacityText.text = $"{TotalCardCount}/{maxCapacity}";
        }
    }

    private void SetCardInteractions(GameObject cardObj, bool enable)
    {
        if (cardObj == null) return;

        CardZoomController czc = cardObj.GetComponent<CardZoomController>();
        if (czc != null) czc.enabled = enable;

        Collider2D[] colliders = cardObj.GetComponentsInChildren<Collider2D>(true);
        foreach (var col in colliders)
        {
            if (col != null) col.enabled = enable;
        }

        ScratchCard[] scratchCards = cardObj.GetComponentsInChildren<ScratchCard>(true);
        foreach (var sc in scratchCards)
        {
            if (sc != null) sc.enabled = enable;
        }
    }

    private void SetCardGroupSortingOrder(GameObject cardObj, int order)
    {
        if (cardObj == null) return;

        SortingGroup sg = cardObj.GetComponent<SortingGroup>();
        if (sg == null) sg = cardObj.AddComponent<SortingGroup>();
        sg.sortingOrder = order;
    }

    public void SetMaxCapacity(int newMax)
    {
        maxCapacity = newMax;
        UpdateCapacityUI();
    }

    public void SetProcessDuration(float newDuration)
    {
        processDuration = newDuration;
    }
}