using UnityEngine;
using System;
using System.Collections.Generic;

public class MultiZoneScratchCard : MonoBehaviour
{
    [Header("Card Data Asset (Optional)")]
    [SerializeField] private StarScratchCardScriptableObject starCardData;
    [SerializeField] private ScratchCardData defaultCardData;

    [Header("Scratch Zones")]
    [SerializeField] private ScratchZone[] zones;

    [Header("Per-Zone Reward Renderers")]
    [SerializeField] private SpriteRenderer[] rewardRenderers;

    [Header("Card Progress Settings")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float overallCompletionThreshold = 0.90f;

    private List<Reward> assignedSpotRewards = new List<Reward>();
    private int totalWinnings = 0;

    public ScratchZone[] Zones => zones;
    public StarScratchCardScriptableObject StarCardData => starCardData;
    public ScratchCardData DefaultCardData => defaultCardData;
    public bool IsCompleted { get; private set; } = false;
    public int TotalWinnings => totalWinnings;

    public Action<int, ScratchZone, Reward> OnZoneRevealedEvent;
    public Action<float> OnCardScratchedEvent;
    public Action<MultiZoneScratchCard> OnAllZonesRevealedEvent;

    private void Awake()
    {
        if (zones == null || zones.Length == 0)
        {
            zones = GetComponentsInChildren<ScratchZone>(true);
        }

        EnsureRewardRenderersExist();

        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] != null)
            {
                zones[i].SetZoneIndex(i);
                zones[i].OnZoneProgress += HandleZoneProgress;
                zones[i].OnZoneRevealed += HandleZoneRevealed;
            }
        }
    }

    private void Start()
    {
        if (assignedSpotRewards.Count == 0)
        {
            if (starCardData != null)
            {
                InitializeFromStarData(starCardData);
            }
            else if (defaultCardData != null && defaultCardData.rewardsList != null && defaultCardData.rewardsList.Count > 0)
            {
                InitializeSpotRewards(defaultCardData.rewardsList);
            }
        }
    }

    private void OnDestroy()
    {
        if (zones != null)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null)
                {
                    zones[i].OnZoneProgress -= HandleZoneProgress;
                    zones[i].OnZoneRevealed -= HandleZoneRevealed;
                }
            }
        }
    }

    public float OverallCompletionThreshold
    {
        get => overallCompletionThreshold;
        set => overallCompletionThreshold = value;
    }

    public void InitializeFromStarData(StarScratchCardScriptableObject data)
    {
        if (data == null) return;
        starCardData = data;
        overallCompletionThreshold = data.overallCardThreshold;

        if (zones != null)
        {
            foreach (var zone in zones)
            {
                if (zone != null)
                {
                    zone.SetRevealThreshold(data.zoneRevealThreshold);
                    if (data.zoneCoverSprite != null)
                    {
                        zone.Initialize(data.zoneCoverSprite);
                    }
                }
            }
        }

        if (data.possibleRewards != null && data.possibleRewards.Count > 0)
        {
            InitializeSpotRewards(data.possibleRewards);
        }
    }

    public void EnsureRewardRenderersExist()
    {
        if (zones == null || zones.Length == 0) return;

        if (rewardRenderers == null || rewardRenderers.Length != zones.Length)
        {
            rewardRenderers = new SpriteRenderer[zones.Length];
        }

        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] == null) continue;

            SpriteRenderer zoneCoverSR = zones[i].GetComponent<SpriteRenderer>();

            // Ensure cover has high enough sorting order (e.g. 5)
            if (zoneCoverSR != null && zoneCoverSR.sortingOrder < 2)
            {
                zoneCoverSR.sortingOrder = 5;
            }

            if (rewardRenderers[i] == null)
            {
                SpriteRenderer[] childSRs = zones[i].GetComponentsInChildren<SpriteRenderer>(true);

                foreach (var sr in childSRs)
                {
                    if (sr != zoneCoverSR)
                    {
                        rewardRenderers[i] = sr;
                        break;
                    }
                }

                if (rewardRenderers[i] == null)
                {
                    GameObject rewardObj = new GameObject($"RewardImage_{i + 1}");
                    rewardObj.transform.SetParent(zones[i].transform, false);
                    rewardObj.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                    rewardObj.transform.localRotation = Quaternion.identity;
                    rewardObj.transform.localScale = Vector3.one;

                    SpriteRenderer newSR = rewardObj.AddComponent<SpriteRenderer>();

                    if (zoneCoverSR != null)
                    {
                        newSR.sortingLayerID = zoneCoverSR.sortingLayerID;
                        newSR.sortingOrder = zoneCoverSR.sortingOrder - 1;
                    }
                    else
                    {
                        newSR.sortingOrder = 1;
                    }

                    rewardRenderers[i] = newSR;
                }
            }
            else
            {
                if (zoneCoverSR != null)
                {
                    rewardRenderers[i].sortingLayerID = zoneCoverSR.sortingLayerID;
                    rewardRenderers[i].sortingOrder = zoneCoverSR.sortingOrder - 1;
                }
            }
        }
    }

    public void InitializeSpotRewards(List<Reward> rewardsPool)
    {
        EnsureRewardRenderersExist();
        assignedSpotRewards.Clear();
        totalWinnings = 0;

        if (rewardsPool == null || rewardsPool.Count == 0)
        {
            Debug.LogWarning("[StarScratchCard] Rewards pool is empty! No rewards will be assigned to spots.");
            return;
        }

        for (int i = 0; i < zones.Length; i++)
        {
            Reward rolledReward = RollWeightedReward(rewardsPool);
            assignedSpotRewards.Add(rolledReward);

            if (rolledReward != null)
            {
                totalWinnings += rolledReward.value;

                if (i < rewardRenderers.Length && rewardRenderers[i] != null)
                {
                    rewardRenderers[i].sprite = rolledReward.rewardSprite;
                    rewardRenderers[i].enabled = true;
                    Debug.Log($"[StarScratchCard] Spot #{i + 1} reward sprite set to: {(rolledReward.rewardSprite != null ? rolledReward.rewardSprite.name : "NULL")} (${rolledReward.value})");
                }
            }
        }

        Debug.Log($"[StarScratchCard] Total potential winnings across all spots: ${totalWinnings}");
    }

    private Reward RollWeightedReward(List<Reward> rewardsPool)
    {
        int totalWeight = 0;
        foreach (var r in rewardsPool)
        {
            if (r != null) totalWeight += Mathf.Max(1, r.weight);
        }

        if (totalWeight <= 0) return rewardsPool[0];

        int rnd = UnityEngine.Random.Range(0, totalWeight);
        int currentSum = 0;

        foreach (var r in rewardsPool)
        {
            if (r == null) continue;
            currentSum += Mathf.Max(1, r.weight);
            if (rnd < currentSum)
            {
                return r;
            }
        }

        return rewardsPool[0];
    }

    private void HandleZoneProgress(int zoneIndex, float progress)
    {
        float overallProgress = GetAverageScratchedPercentage();
        OnCardScratchedEvent?.Invoke(overallProgress);

        if (!IsCompleted && overallProgress >= overallCompletionThreshold)
        {
            CheckCardCompletion();
        }
    }

    private void HandleZoneRevealed(int zoneIndex, ScratchZone zone)
    {
        Reward spotReward = (zoneIndex < assignedSpotRewards.Count) ? assignedSpotRewards[zoneIndex] : null;
        OnZoneRevealedEvent?.Invoke(zoneIndex, zone, spotReward);
        CheckCardCompletion();
    }

    private void CheckCardCompletion()
    {
        if (IsCompleted) return;

        if (AreAllZonesRevealed() || GetAverageScratchedPercentage() >= overallCompletionThreshold)
        {
            IsCompleted = true;
            Debug.Log($"[MultiZoneScratchCard] All spots on '{gameObject.name}' revealed! Total winnings: ${totalWinnings}");
            OnAllZonesRevealedEvent?.Invoke(this);
        }
    }

    public float GetAverageScratchedPercentage()
    {
        if (zones == null || zones.Length == 0) return 0f;

        float sum = 0f;
        int count = 0;
        foreach (var zone in zones)
        {
            if (zone != null)
            {
                sum += zone.GetScratchedPercentage();
                count++;
            }
        }

        return count > 0 ? sum / count : 0f;
    }

    public bool AreAllZonesRevealed()
    {
        if (zones == null || zones.Length == 0) return false;

        foreach (var zone in zones)
        {
            if (zone != null && !zone.IsRevealed)
            {
                return false;
            }
        }

        return true;
    }
}
