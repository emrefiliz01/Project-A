using UnityEngine;
using System.Collections;
using DG.Tweening;

public class ScratchRobot : MonoBehaviour
{
    public static ScratchRobot Instance { get; private set; }

    [Header("Robot Settings")]
    [SerializeField] private float processDuration = 3f;

    [SerializeField] private Transform cardHoldingSpot;

    [SerializeField] private Transform cardThrowSpot;

    [Header("Animation FX")]
    [SerializeField] private float shakeStrength = 0.15f;
    [SerializeField] private Vector3 cardProcessingScale = new Vector3(0.5f, 0.5f, 1f);

    private bool isProcessing = false;
    private GameObject currentProcessingCard;

    public bool IsProcessing => isProcessing;

    private void Awake()
    {
        Instance = this;
    }

    public bool AcceptCard(GameObject cardObj)
    {
        if (isProcessing || cardObj == null) return false;

        StartCoroutine(ProcessCardRoutine(cardObj));
        return true;
    }

    private IEnumerator ProcessCardRoutine(GameObject cardObj)
    {
        isProcessing = true;
        currentProcessingCard = cardObj;

        Vector3 originalScale = cardObj.transform.localScale;

        CardZoomController czc = cardObj.GetComponent<CardZoomController>();
        if (czc != null)
        {
            czc.enabled = false;
        }

        Vector3 holdPos = cardHoldingSpot != null ? cardHoldingSpot.position : transform.position;

        cardObj.transform.DOMove(holdPos, 0.4f).SetEase(Ease.OutQuad);
        cardObj.transform.DOScale(cardProcessingScale, 0.4f);

        yield return new WaitForSeconds(0.4f);

        transform.DOShakePosition(processDuration, shakeStrength, 20);

        yield return new WaitForSeconds(processDuration);

        ScratchCard[] scratchCards = cardObj.GetComponentsInChildren<ScratchCard>(true);
        foreach (var sc in scratchCards)
        {
            if (sc != null)
            {
                sc.ClearAll();
            }
        }

        Vector3 throwPos = cardThrowSpot != null ? cardThrowSpot.position : transform.position + new Vector3(2.5f, -1.5f, 0f);

        cardObj.transform.DOScale(originalScale, 0.4f);
        cardObj.transform.DOMove(throwPos, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            if (czc != null)
            {
                czc.SetHomePosition(throwPos, cardObj.transform.rotation, originalScale);
                czc.enabled = true;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCardZoomedOut(cardObj);
            }
        });

        isProcessing = false;
        currentProcessingCard = null;
    }
}