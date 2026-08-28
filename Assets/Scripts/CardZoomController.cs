using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class CardZoomController : MonoBehaviour
{
    public static CardZoomController CurrentlyZoomedCard { get; private set; }

    [Header("Zoom Animation Settings")]
    [SerializeField] private Vector3 targetPosition = new Vector3(0f, 0f, -2f);
    [SerializeField] private Vector3 targetScale = Vector3.one;
    [SerializeField] private Vector3 targetRotation = Vector3.zero;
    [SerializeField] private float zoomDuration = 1.2f;
    [SerializeField] private Ease easeType = Ease.OutQuad;
    [SerializeField] private int sortingOrderBoost = 100;

    [Header("Table Drag Settings")]
    [SerializeField] private float dragThresholdPixels = 10f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;

    private ScratchCard[] scratchCards;
    private bool isZoomedIn = false;
    private bool isAnimating = false;

    // Drag tracking
    private bool isDragging = false;
    private Vector3 mouseDownScreenPos;
    private Vector3 dragOffset;

    private Dictionary<SpriteRenderer, int> originalSpriteOrders = new Dictionary<SpriteRenderer, int>();
    private Dictionary<Renderer, int> originalRendererOrders = new Dictionary<Renderer, int>();

    public bool IsZoomedIn => isZoomedIn;
    public bool IsAnimating => isAnimating;

    private void Awake()
    {
        scratchCards = GetComponentsInChildren<ScratchCard>(true);
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;

        if (GetComponent<Collider2D>() == null && GetComponentInChildren<Collider2D>() == null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = sr.size != Vector2.zero ? sr.size : (Vector2)sr.bounds.size;
            }
        }
        
        SetScratchableState(false);
    }

    private void Update()
    {
        if (isZoomedIn && !isAnimating && CurrentlyZoomedCard == this)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsClickOutsideCardAndUI())
                {
                    ZoomToTableView();
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (CurrentlyZoomedCard == this)
        {
            CurrentlyZoomedCard = null;
            if (CardInfoPanelUI.Instance != null)
            {
                CardInfoPanelUI.Instance.HidePanel();
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetNormalCursor();
            }
        }
    }

    public void SetHomePosition(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        initialPosition = position;
        initialRotation = rotation;
        initialScale = scale;
    }

    private void OnMouseDown()
    {
        if (isAnimating || isZoomedIn) return;

        mouseDownScreenPos = Input.mousePosition;
        Vector3 mouseWorld = GetMouseWorldPosition();
        dragOffset = transform.position - new Vector3(mouseWorld.x, mouseWorld.y, transform.position.z);
        isDragging = false;
    }

    private void OnMouseDrag()
    {
        if (isAnimating || isZoomedIn) return;

        if (!isDragging && Vector3.Distance(Input.mousePosition, mouseDownScreenPos) > dragThresholdPixels)
        {
            isDragging = true;
            BoostSortingOrder();
        }

        if (isDragging)
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            transform.position = new Vector3(mouseWorld.x + dragOffset.x, mouseWorld.y + dragOffset.y, initialPosition.z);
        }
    }

private void OnMouseUp()
    {
        if (isAnimating || isZoomedIn) return;

        if (isDragging)
        {
            isDragging = false;
            RestoreSortingOrder();

            // 1. ROBOT KONTROLÜ
            if (IsOverRobot(out ScratchRobot robot))
            {
                if (robot.AcceptCard(gameObject))
                {
                    return;
                }
            }

            if (IsOverTrashBin())
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.DiscardCard(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                SetHomePosition(transform.position, initialRotation, initialScale);
            }
        }
        else
        {

            if (CurrentlyZoomedCard != null && CurrentlyZoomedCard != this)
            {
                return;
            }

            CardZoomController unfinished = GetActiveUnfinishedCard();
            if (unfinished != null && unfinished != this)
            {
                unfinished.ZoomToScratchMode();
                return;
            }

            ZoomToScratchMode();
        }
    }

    public void HandleChildMouseDown() => OnMouseDown();
    public void HandleChildMouseDrag() => OnMouseDrag();
    public void HandleChildMouseUp() => OnMouseUp();

    public void ZoomToScratchMode()
    {
        if (isAnimating || isZoomedIn) return;
        if (CurrentlyZoomedCard != null && CurrentlyZoomedCard != this) return;

        CurrentlyZoomedCard = this;
        isAnimating = true;
        SetScratchableState(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetScratchCursor();
            GameManager.Instance.OnCardZoomedIn(gameObject);
        }

        if (CardInfoPanelUI.Instance != null)
        {
            CardInfoPanelUI.Instance.ShowPanelForCard(gameObject);
        }

        BoostSortingOrder();

        transform.DOMove(targetPosition, zoomDuration).SetEase(easeType);
        transform.DORotate(targetRotation, zoomDuration).SetEase(easeType);

        transform.DOScale(targetScale, zoomDuration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                isZoomedIn = true;
                isAnimating = false;
                SetScratchableState(true);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnCardZoomedIn(gameObject);
                }
            });
    }

    public void ZoomToTableView()
    {
        if (isAnimating || !isZoomedIn) return;

        isAnimating = true;
        SetScratchableState(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetNormalCursor();
            GameManager.Instance.OnCardZoomedOut(gameObject);
        }

        if (CardInfoPanelUI.Instance != null)
        {
            CardInfoPanelUI.Instance.HidePanel();
        }

        transform.DOMove(initialPosition, zoomDuration).SetEase(easeType);
        transform.DORotateQuaternion(initialRotation, zoomDuration).SetEase(easeType);

        transform.DOScale(initialScale, zoomDuration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                isZoomedIn = false;
                isAnimating = false;
                RestoreSortingOrder();

                if (CurrentlyZoomedCard == this)
                {
                    CurrentlyZoomedCard = null;
                }
            });
    }

    private void BoostSortingOrder()
    {
        originalSpriteOrders.Clear();
        originalRendererOrders.Clear();

        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in spriteRenderers)
        {
            if (sr != null)
            {
                originalSpriteOrders[sr] = sr.sortingOrder;
                sr.sortingOrder += sortingOrderBoost;
            }
        }

        Renderer[] otherRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in otherRenderers)
        {
            if (r != null && !(r is SpriteRenderer))
            {
                originalRendererOrders[r] = r.sortingOrder;
                r.sortingOrder += sortingOrderBoost;
            }
        }
    }

    private void RestoreSortingOrder()
    {
        foreach (var kvp in originalSpriteOrders)
        {
            if (kvp.Key != null)
            {
                kvp.Key.sortingOrder = kvp.Value;
            }
        }
        originalSpriteOrders.Clear();

        foreach (var kvp in originalRendererOrders)
        {
            if (kvp.Key != null)
            {
                kvp.Key.sortingOrder = kvp.Value;
            }
        }
        originalRendererOrders.Clear();
    }

    private void SetScratchableState(bool state)
    {
        if (scratchCards != null)
        {
            foreach (var card in scratchCards)
            {
                if (card != null)
                {
                    card.IsScratchable = state;
                }
            }
        }
    }


    public bool IsUnfinished()
    {
        if (this == null || gameObject == null) return false;

        MultiZoneScratchCard multiCard = GetComponent<MultiZoneScratchCard>();
        if (multiCard != null)
        {
            if (multiCard.IsCompleted) return false;
            return multiCard.HasAnyZoneRevealed || multiCard.GetAverageScratchedPercentage() > 0.01f;
        }

        ScratchCard sc = GetComponentInChildren<ScratchCard>();
        if (sc != null)
        {
            if (sc.IsCompleted) return false;
            return sc.GetScratchedPercentage() > 0.01f || (sc.UseLocalizedRewardCheck && sc.GetSymbolScratchedPercentage() > 0.01f);
        }

        return false;
    }

    public static CardZoomController GetActiveUnfinishedCard()
    {
        if (GameManager.Instance != null && GameManager.Instance.ActiveCards != null)
        {
            foreach (var cardObj in GameManager.Instance.ActiveCards)
            {
                if (cardObj != null)
                {
                    CardZoomController czc = cardObj.GetComponent<CardZoomController>();
                    if (czc != null && czc.IsUnfinished()) return czc;
                }
            }
        }

        CardZoomController[] allCards = FindObjectsOfType<CardZoomController>();
        foreach (var card in allCards)
        {
            if (card != null && card.IsUnfinished())
            {
                return card;
            }
        }
        return null;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null) return transform.position;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    private bool IsClickOutsideCardAndUI()
    {
        if (Camera.main == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            {
                return false;
            }

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CollectRewardButton != null && hit.collider.gameObject == GameManager.Instance.CollectRewardButton)
                    return false;
                if (GameManager.Instance.TrashBinButton != null && hit.collider.gameObject == GameManager.Instance.TrashBinButton)
                    return false;
            }
        }

        return true;
    }

    private bool IsOverTrashBin()
    {
        if (Camera.main == null) return false;

        // 1. Raycast / Overlap at mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);
        foreach (var hit in hits)
        {
            if (hit.collider != null && IsTrashBinObject(hit.collider.gameObject))
                return true;
        }

        Vector3 mouseWorld = GetMouseWorldPosition();
        Collider2D[] pointHits = Physics2D.OverlapPointAll(new Vector2(mouseWorld.x, mouseWorld.y));
        foreach (var col in pointHits)
        {
            if (col != null && IsTrashBinObject(col.gameObject))
                return true;
        }

        // 2. Card colliders overlap with trash bin
        Collider2D[] myColliders = GetComponentsInChildren<Collider2D>();
        foreach (var myCol in myColliders)
        {
            if (myCol == null || !myCol.enabled) continue;
            Collider2D[] results = new Collider2D[10];
            ContactFilter2D filter = new ContactFilter2D().NoFilter();
            int count = myCol.OverlapCollider(filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i] != null && IsTrashBinObject(results[i].gameObject))
                    return true;
            }
        }

        return false;
    }

    private bool IsTrashBinObject(GameObject obj)
    {
        if (obj == null) return false;
        if (obj.CompareTag("TrashBin")) return true;
        if (GameManager.Instance != null && GameManager.Instance.TrashBinButton == obj) return true;
        if (obj.name.IndexOf("Trash", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

private bool IsOverRobot(out ScratchRobot robot)
    {
        robot = null;
        if (Camera.main == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);
        foreach (var hit in hits)
        {
            if (hit.collider != null)
            {
                ScratchRobot r = hit.collider.GetComponent<ScratchRobot>();
                if (r == null) r = hit.collider.GetComponentInParent<ScratchRobot>();

                if (r != null || hit.collider.CompareTag("ScratchRobot"))
                {
                    robot = r != null ? r : ScratchRobot.Instance;
                    
                    return robot != null && !robot.IsProcessing;
                }
            }
        }
        return false;
    }
}

