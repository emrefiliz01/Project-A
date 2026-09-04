using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public class LunaController : MonoBehaviour, IPointerClickHandler
{
    [Header("Paw & Visual References")]
    [Tooltip("Pati objesi (boş bırakılırsa Luna objesinin kendisi sallanır)")]
    [SerializeField] private Transform pawTransform;
    [SerializeField] private SpriteRenderer lunaSpriteRenderer;

    [Header("Reward Floating Text")]
    [Tooltip("Drag and drop your 'Luna'sCollectedRewardsText' TMP component here")]
    [SerializeField] private TMP_Text lunasCollectedRewardsText;
    [SerializeField] private float textDisplayDuration = 1.0f;
    [SerializeField] private float textFadeDuration = 0.5f;
    [SerializeField] private Color rewardTextColor = Color.yellow;

    [Header("Wave Animation Settings")]
    [Tooltip("Tek bir pati sallama döngüsünün toplam süresi (aşağı iniş + yukarı çıkış)")]
    [SerializeField] private float waveCycleDuration = 0.9f;

    [Tooltip("Patinin veya gövdenin en alt noktaya indiğindeki dönüş açısı (Z ekseni)")]
    [SerializeField] private float waveRotationAngle = -18f;

    [Tooltip("Patinin veya gövdenin en alt noktaya indiğindeki dikey kayma miktarı")]
    [SerializeField] private float wavePositionOffsetY = -0.15f;

    [Header("Visual Feedback Settings")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private float clickPunchScale = 0.12f;

    // Runtime state
    private bool isToggledOn = false;
    private bool isWaveRoutineRunning = false;
    private bool isPausedAtTop = false;

    private Vector3 initialPawLocalPos;
    private Quaternion initialPawLocalRot;
    private Coroutine waveCoroutine;
    private Sequence textAnimationSequence;

    public bool IsToggledOn => isToggledOn;

    private void Awake()
    {
        if (lunaSpriteRenderer == null)
        {
            lunaSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        Transform targetAnimTransform = pawTransform != null ? pawTransform : transform;
        initialPawLocalPos = targetAnimTransform.localPosition;
        initialPawLocalRot = targetAnimTransform.localRotation;

        // Ensure collider exists for mouse interaction
        if (GetComponent<Collider2D>() == null)
        {
            SpriteRenderer sr = lunaSpriteRenderer != null ? lunaSpriteRenderer : GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = sr.size != Vector2.zero ? sr.size : (Vector2)sr.bounds.size;
            }
        }

        if (lunasCollectedRewardsText != null)
        {
            lunasCollectedRewardsText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        SetVisualState(false);
    }

    private void OnEnable()
    {
        ScratchRobot.OnCardProcessed += HandleCardProcessedByRobot;
    }

    private void OnDisable()
    {
        ScratchRobot.OnCardProcessed -= HandleCardProcessedByRobot;
        StopWaveCycle();
    }

    private void Update()
    {
        // If toggled ON and paused at top, check if any completed card appeared on the table
        if (isToggledOn && isPausedAtTop && !isWaveRoutineRunning)
        {
            if (HasAnyCompletedCardOnTable())
            {
                ResumeWaveCycle();
            }
        }
    }

    #region Mouse / Pointer Handlers
    private void OnMouseDown()
    {
        Toggle();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }
    #endregion

    #region State & Toggle
    public void Toggle()
    {
        SetToggleState(!isToggledOn);
    }

    public void SetToggleState(bool turnOn)
    {
        isToggledOn = turnOn;
        SetVisualState(isToggledOn);

        // Click feedback punch
        transform.DOKill();
        transform.DOPunchScale(Vector3.one * clickPunchScale, 0.2f, 8, 1);

        if (isToggledOn)
        {
            StartWaveCycle();
        }
        else
        {
            StopWaveCycle();
        }
    }

    private void SetVisualState(bool on)
    {
        if (lunaSpriteRenderer != null)
        {
            lunaSpriteRenderer.color = on ? activeColor : inactiveColor;
        }
    }
    #endregion

    #region Paw Waving & Auto Collection Cycle
    private void StartWaveCycle()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }
        isWaveRoutineRunning = true;
        isPausedAtTop = false;
        waveCoroutine = StartCoroutine(WaveAnimationRoutine());
    }

    private void StopWaveCycle()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        isWaveRoutineRunning = false;
        isPausedAtTop = false;

        Transform targetAnimTransform = pawTransform != null ? pawTransform : transform;
        targetAnimTransform.DOKill();
        targetAnimTransform.DOLocalMove(initialPawLocalPos, 0.2f).SetEase(Ease.OutQuad);
        targetAnimTransform.DOLocalRotateQuaternion(initialPawLocalRot, 0.2f).SetEase(Ease.OutQuad);
    }

    private void ResumeWaveCycle()
    {
        if (!isToggledOn) return;
        isPausedAtTop = false;
        if (!isWaveRoutineRunning)
        {
            StartWaveCycle();
        }
    }

    private IEnumerator WaveAnimationRoutine()
    {
        Transform targetAnimTransform = pawTransform != null ? pawTransform : transform;
        float halfDuration = Mathf.Max(0.1f, waveCycleDuration / 2f);

        Vector3 downPos = initialPawLocalPos + new Vector3(0f, wavePositionOffsetY, 0f);
        Quaternion downRot = initialPawLocalRot * Quaternion.Euler(0f, 0f, waveRotationAngle);

        while (isToggledOn)
        {
            // 1. Downward movement (Paw moves down)
            targetAnimTransform.DOLocalMove(downPos, halfDuration).SetEase(Ease.InOutSine);
            targetAnimTransform.DOLocalRotateQuaternion(downRot, halfDuration).SetEase(Ease.InOutSine);

            yield return new WaitForSeconds(halfDuration);

            if (!isToggledOn) break;

            // 2. TRIGGER POINT: Collect topmost completed card at lowest stroke
            TryCollectCompletedCard();

            // 3. Upward movement (Paw returns to top)
            targetAnimTransform.DOLocalMove(initialPawLocalPos, halfDuration).SetEase(Ease.InOutSine);
            targetAnimTransform.DOLocalRotateQuaternion(initialPawLocalRot, halfDuration).SetEase(Ease.InOutSine);

            yield return new WaitForSeconds(halfDuration);

            if (!isToggledOn) break;

            // 4. Pause check: Any completed cards remaining on the table?
            if (!HasAnyCompletedCardOnTable())
            {
                isPausedAtTop = true;
                isWaveRoutineRunning = false;
                waveCoroutine = null;
                yield break;
            }
        }

        isWaveRoutineRunning = false;
        waveCoroutine = null;
    }

    private void TryCollectCompletedCard()
    {
        if (GameManager.Instance == null) return;

        List<GameObject> completedCards = GameManager.Instance.GetCompletedCards();
        if (completedCards != null && completedCards.Count > 0)
        {
            // Iterate BACKWARDS to pick the TOPMOST / LAST card thrown onto the table
            for (int i = completedCards.Count - 1; i >= 0; i--)
            {
                GameObject card = completedCards[i];
                if (card != null)
                {
                    // Skip cards that are inside or queued in the robot
                    if (ScratchRobot.Instance != null && ScratchRobot.Instance.IsCardInRobot(card)) continue;

                    // Calculate scaled reward before collecting
                    int rawReward = 0;
                    MultiZoneScratchCard mc = card.GetComponent<MultiZoneScratchCard>();
                    RewardManager rm = card.GetComponent<RewardManager>();

                    if (mc != null) rawReward = mc.CalculateRevealedWinnings();
                    else if (rm != null) rawReward = rm.ActiveRewardValue;

                    int scaledReward = GameManager.Instance.GetScaledRewardForCard(card, rawReward);

                    // Collect card & show visual floating text
                    if (GameManager.Instance.CollectCardDirectly(card))
                    {
                        ShowCollectedRewardText(scaledReward);
                        break;
                    }
                }
            }
        }
    }

    private void ShowCollectedRewardText(int amount)
    {
        if (lunasCollectedRewardsText == null) return;

        // Kill existing text animation
        if (textAnimationSequence != null && textAnimationSequence.IsActive())
        {
            textAnimationSequence.Kill();
        }

        // Set text content
        string prefix = amount >= 0 ? "+" : "";
        lunasCollectedRewardsText.text = prefix + CurrencyFormatter.FormatMoney(amount);

        // Reset color to Yellow with 100% Alpha
        Color col = rewardTextColor;
        col.a = 1f;
        lunasCollectedRewardsText.color = col;
        lunasCollectedRewardsText.gameObject.SetActive(true);

        // Sequence: Display for 1 second, then fade out
        textAnimationSequence = DOTween.Sequence();
        textAnimationSequence.AppendInterval(textDisplayDuration);
        textAnimationSequence.Append(lunasCollectedRewardsText.DOFade(0f, textFadeDuration));
        textAnimationSequence.OnComplete(() =>
        {
            lunasCollectedRewardsText.gameObject.SetActive(false);
        });
    }

    private bool HasAnyCompletedCardOnTable()
    {
        if (GameManager.Instance == null) return false;
        List<GameObject> completedCards = GameManager.Instance.GetCompletedCards();
        if (completedCards == null || completedCards.Count == 0) return false;

        for (int i = 0; i < completedCards.Count; i++)
        {
            GameObject card = completedCards[i];
            if (card != null)
            {
                if (ScratchRobot.Instance != null && ScratchRobot.Instance.IsCardInRobot(card)) continue;
                return true;
            }
        }
        return false;
    }

    private void HandleCardProcessedByRobot(GameObject card)
    {
        // Wake Luna up immediately if a new card is thrown on table
        if (isToggledOn && isPausedAtTop)
        {
            ResumeWaveCycle();
        }
    }
    #endregion
}