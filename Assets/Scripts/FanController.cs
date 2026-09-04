using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

public class FanController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Blowing Settings")]
    [Tooltip("Masadaki kartların robota doğru çekilme hızı")]
    [SerializeField] private float blowSpeed = 5.0f;

    [Tooltip("Kartın robota girmesi için gereken yakınlık mesafesi")]
    [SerializeField] private float intakeDistance = 0.8f;

    [Tooltip("Robot doluyken kartların durup bekleyeceği giriş önü mesafesi")]
    [SerializeField] private float queueStopDistance = 1.0f;

    [Header("Fan Visual & FX Settings")]
    [SerializeField] private float blowShakeStrength = 0.08f;
    [SerializeField] private float blowScalePunch = 0.05f;

    private bool isBlowing = false;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Tween shakeTween;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;

        // Ensure collider exists for mouse interaction
        if (GetComponent<Collider2D>() == null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = sr.size != Vector2.zero ? sr.size : (Vector2)sr.bounds.size;
            }
        }
    }

    private void Update()
    {
        // Global mouse button release check in case cursor dragged off collider
        if (isBlowing && !Input.GetMouseButton(0))
        {
            StopBlowing();
        }

        if (isBlowing)
        {
            ProcessBlowing();
        }
    }

    private void OnDisable()
    {
        StopBlowing();
    }

    #region Mouse / Pointer Handlers
    private void OnMouseDown()
    {
        StartBlowing();
    }

    private void OnMouseUp()
    {
        StopBlowing();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StartBlowing();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopBlowing();
    }
    #endregion

    #region Blowing Logic
    public void StartBlowing()
    {
        if (isBlowing) return;
        isBlowing = true;

        // Visual animation on Fan
        transform.DOKill();
        transform.DOScale(originalScale * (1f + blowScalePunch), 0.2f).SetEase(Ease.OutQuad);
        shakeTween = transform.DOShakePosition(1000f, blowShakeStrength, 20, 90f, false, false)
            .SetEase(Ease.Linear);
    }

    public void StopBlowing()
    {
        if (!isBlowing) return;
        isBlowing = false;

        if (shakeTween != null && shakeTween.IsActive())
        {
            shakeTween.Kill();
        }

        transform.DOKill();
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad);
        transform.DOMove(originalPosition, 0.2f).SetEase(Ease.OutQuad);
    }

    private void ProcessBlowing()
    {
        if (ScratchRobot.Instance == null || GameManager.Instance == null) return;

        Vector3 intakePos = ScratchRobot.Instance.IntakeSpot.position;
        List<GameObject> activeCards = GameManager.Instance.ActiveCards;
        if (activeCards == null || activeCards.Count == 0) return;

        // Clone list to safely iterate
        List<GameObject> cardsToCheck = new List<GameObject>(activeCards);

        for (int i = 0; i < cardsToCheck.Count; i++)
        {
            GameObject cardObj = cardsToCheck[i];
            if (cardObj == null) continue;

            // Check if card is unscratched/unprocessed and not currently zoomed in
            if (!IsCardEligibleForFan(cardObj)) continue;

            Vector3 cardPos = cardObj.transform.position;
            float distanceToIntake = Vector2.Distance(cardPos, intakePos);

            bool isRobotFull = ScratchRobot.Instance.IsFull;

            // 1. Kart robota yeterince yaklaştı mı?
            if (distanceToIntake <= intakeDistance)
            {
                if (!isRobotFull)
                {
                    // Robot boşsa kartı besle
                    ScratchRobot.Instance.AcceptCard(cardObj);
                    continue;
                }
            }

            // 2. Eğer robot doluysa ve kart bekleme mesafesindeyse dur
            if (isRobotFull && distanceToIntake <= queueStopDistance)
            {
                // Kart robotun girişinde bekler
                continue;
            }

            // 3. Kartı robota doğru pürüzsüzce hareket ettir
            Vector3 targetPos = Vector3.MoveTowards(cardPos, intakePos, blowSpeed * Time.deltaTime);
            targetPos.z = cardPos.z; // Maintain original Z depth
            cardObj.transform.position = targetPos;

            // Kartın Home Position'ını güncelle ki bırakıldığında veya zoom yapıldığında yeni yerini bilsin
            CardZoomController czc = cardObj.GetComponent<CardZoomController>();
            if (czc != null)
            {
                czc.SetHomePosition(targetPos, cardObj.transform.rotation, cardObj.transform.localScale);
            }
        }
    }

    /// <summary>
    /// Kartın masada duran, henüz kazınmamış ve zoom modunda olmayan bir kart olup olmadığını kontrol eder.
    /// </summary>
    private bool IsCardEligibleForFan(GameObject cardObj)
    {
        if (cardObj == null) return false;

        // Robotun içinde veya kuyruğunda olan kartları Fan üflemez
        if (ScratchRobot.Instance != null && ScratchRobot.Instance.IsCardInRobot(cardObj)) return false;

        // Kazınmış/Tamamlanmış kartları Fan üflemez
        if (ScratchRobot.IsCardScratched(cardObj)) return false;
        if (GameManager.Instance != null && GameManager.Instance.IsCardCompleted(cardObj)) return false;

        // Zoom modundaki kartları Fan üflemez
        CardZoomController czc = cardObj.GetComponent<CardZoomController>();
        if (czc != null)
        {
            if (czc.IsZoomedIn || czc.IsAnimating || CardZoomController.CurrentlyZoomedCard == czc)
            {
                return false;
            }
        }

        return true;
    }
    #endregion
}
