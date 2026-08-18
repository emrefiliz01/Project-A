using UnityEngine;
using System;

/// <summary>
/// Attached to an individual circular scratch spot (e.g. Spot 1, Spot 2, Spot 3).
/// Inherits from ScratchCard and tracks per-zone reveal progress.
/// </summary>
public class ScratchZone : ScratchCard
{
    [Header("Zone Configuration")]
    [SerializeField] private int zoneIndex = 0;
    [Range(0.1f, 1.0f)]
    [SerializeField] private float revealThreshold = 0.80f;
    [SerializeField] private bool autoClearOnReveal = true;

    public int ZoneIndex => zoneIndex;
    public float RevealThreshold => revealThreshold;
    public bool IsRevealed { get; private set; } = false;

    // Events: (zoneIndex, currentPercentage) and (zoneIndex, scratchZone)
    public Action<int, float> OnZoneProgress;
    public Action<int, ScratchZone> OnZoneRevealed;

    public void SetZoneIndex(int index)
    {
        zoneIndex = index;
    }

    public void SetRevealThreshold(float threshold)
    {
        revealThreshold = Mathf.Clamp(threshold, 0.1f, 1.0f);
    }

    private void Awake()
    {
        OnScratched += HandleScratched;
    }

    private void OnDestroy()
    {
        OnScratched -= HandleScratched;
    }

    private void HandleScratched(float percentage)
    {
        OnZoneProgress?.Invoke(zoneIndex, percentage);

        if (!IsRevealed && percentage >= revealThreshold)
        {
            IsRevealed = true;
            Debug.Log($"[ScratchZone] Spot #{zoneIndex + 1} Revealed! ({Mathf.FloorToInt(percentage * 100)}% scratched)");

            if (autoClearOnReveal)
            {
                ClearAll();
            }

            OnZoneRevealed?.Invoke(zoneIndex, this);
        }
    }
}
