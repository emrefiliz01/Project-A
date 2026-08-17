using UnityEngine;
using DG.Tweening;

public class CardZoomController : MonoBehaviour
{
    [SerializeField] private Vector3 targetPosition = Vector3.zero;
    [SerializeField] private Vector3 targetScale = Vector3.one;
    [SerializeField] private Vector3 targetRotation = Vector3.zero;
    [SerializeField] private float zoomDuration = 1.2f;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;

    private ScratchCard scratchCard;
    private bool isZoomedIn = false;
    private bool isAnimating = false;

    private void Awake()
    {
        // Finds the ScratchCard component on this GameObject or in any of its children
        scratchCard = GetComponentInChildren<ScratchCard>();
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
        
        if (scratchCard != null)
        {
            scratchCard.IsScratchable = false;
        }
    }

    /// <summary>
    /// Updates the "home" position/rotation/scale that ZoomToTableView returns to.
    /// Call this after moving the card to its final resting place (e.g. after a DOMove).
    /// </summary>
    public void SetHomePosition(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        initialPosition = position;
        initialRotation = rotation;
        initialScale = scale;
    }

    private void OnMouseDown()
    {
        if (isAnimating || isZoomedIn) return;

        ZoomToScratchMode();
    }

    public void ZoomToScratchMode()
    {
        if (isAnimating || isZoomedIn) return;

        isAnimating = true;
        
        if (scratchCard != null)
        {
            scratchCard.IsScratchable = false;
        }

        transform.DOMove(targetPosition, zoomDuration).SetEase(easeType);
        transform.DORotate(targetRotation, zoomDuration).SetEase(easeType);

        transform.DOScale(targetScale, zoomDuration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                isZoomedIn = true;
                isAnimating = false;
                
                if (scratchCard != null)
                {
                    scratchCard.IsScratchable = true;
                }
            });
    }

    public void ZoomToTableView()
    {
        if (isAnimating || !isZoomedIn) return;

        isAnimating = true;
        
        if (scratchCard != null)
        {
            scratchCard.IsScratchable = false;
        }

        transform.DOMove(initialPosition, zoomDuration).SetEase(easeType);
        transform.DORotateQuaternion(initialRotation, zoomDuration).SetEase(easeType);

        transform.DOScale(initialScale, zoomDuration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                isZoomedIn = false;
                isAnimating = false;
            });
    }
}
