using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class CardZoomController : MonoBehaviour
{
    public static CardZoomController CurrentlyZoomedCard { get; private set; }

    [SerializeField] private Vector3 targetPosition = new Vector3(0f, 0f, -2f);
    [SerializeField] private Vector3 targetScale = Vector3.one;
    [SerializeField] private Vector3 targetRotation = Vector3.zero;
    [SerializeField] private float zoomDuration = 1.2f;
    [SerializeField] private Ease easeType = Ease.OutQuad;
    [SerializeField] private int sortingOrderBoost = 100;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;

    private ScratchCard[] scratchCards;
    private bool isZoomedIn = false;
    private bool isAnimating = false;

    private Dictionary<SpriteRenderer, int> originalSpriteOrders = new Dictionary<SpriteRenderer, int>();
    private Dictionary<Renderer, int> originalRendererOrders = new Dictionary<Renderer, int>();

    private void Awake()
    {
        scratchCards = GetComponentsInChildren<ScratchCard>(true);
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
        
        SetScratchableState(false);
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

        if (CurrentlyZoomedCard != null && CurrentlyZoomedCard != this)
        {
            Debug.Log("[CardZoomController] Another card is currently active! Finish scratching or zooming out first.");
            return;
        }

        ZoomToScratchMode();
    }

    public void ZoomToScratchMode()
    {
        if (isAnimating || isZoomedIn) return;
        if (CurrentlyZoomedCard != null && CurrentlyZoomedCard != this) return;

        CurrentlyZoomedCard = this;
        isAnimating = true;
        SetScratchableState(false);

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
            });
    }

    public void ZoomToTableView()
    {
        if (isAnimating || !isZoomedIn) return;

        isAnimating = true;
        SetScratchableState(false);

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
}
