using UnityEngine;
using System;
using System.Collections.Generic;

public class MultiZoneScratchCard : MonoBehaviour
{
    [Header("Card Data Assets (Optional)")]
    [SerializeField] private QuickCashCardScriptableObject quickCashCardData;
    [SerializeField] private AppleTreeCardScriptableObject appleTreeCardData;
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
    public QuickCashCardScriptableObject QuickCashCardData => quickCashCardData;
    public AppleTreeCardScriptableObject AppleTreeCardData => appleTreeCardData;
    public StarScratchCardScriptableObject StarCardData => starCardData;
    public ScratchCardData DefaultCardData => defaultCardData;
    public bool IsCompleted { get; private set; } = false;
    public int TotalWinnings => CalculateRevealedWinnings();
    public int RevealedWinnings => CalculateRevealedWinnings();

    public bool HasAnyZoneRevealed
    {
        get
        {
            if (zones == null || zones.Length == 0) return false;
            foreach (var zone in zones)
            {
                if (zone != null && zone.IsRevealed) return true;
            }
            return false;
        }
    }

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
            if (quickCashCardData != null)
            {
                InitializeFromQuickCashData(quickCashCardData);
            }
            else if (appleTreeCardData != null)
            {
                InitializeFromAppleTreeData(appleTreeCardData);
            }
            else if (starCardData != null)
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

    public void InitializeFromQuickCashData(QuickCashCardScriptableObject data)
    {
        if (data == null) return;
        quickCashCardData = data;
        overallCompletionThreshold = data.overallCardThreshold;

        if (zones != null)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null)
                {
                    zones[i].SetRevealThreshold(data.zoneRevealThreshold);

                    // Son dizilim elemanı (Bonus Spot) için bonusZoneCoverSprite kullan
                    if (i == zones.Length - 1 && data.bonusZoneCoverSprite != null)
                    {
                        zones[i].Initialize(data.bonusZoneCoverSprite);
                    }
                    else if (data.zoneCoverSprite != null)
                    {
                        zones[i].Initialize(data.zoneCoverSprite);
                    }
                }
            }
        }

        if (data.possibleRewards != null && data.possibleRewards.Count > 0)
        {
            InitializeSpotRewardsWithMax3Constraint(data.possibleRewards);
        }
    }

    public void InitializeFromAppleTreeData(AppleTreeCardScriptableObject data)
    {
        if (data == null) return;
        appleTreeCardData = data;
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

    public void InitializeSpotRewardsWithMax3Constraint(List<Reward> rewardsPool)
    {
        EnsureRewardRenderersExist();
        assignedSpotRewards.Clear();
        totalWinnings = 0;

        if (rewardsPool == null || rewardsPool.Count == 0) return;

        Dictionary<string, int> rolledCounts = new Dictionary<string, int>();

        for (int i = 0; i < zones.Length; i++)
        {
            List<Reward> validPool = new List<Reward>();
            foreach (var r in rewardsPool)
            {
                if (r == null) continue;
                string key = !string.IsNullOrEmpty(r.rewardName) ? r.rewardName : r.value.ToString();
                if (!rolledCounts.ContainsKey(key) || rolledCounts[key] < 3)
                {
                    validPool.Add(r);
                }
            }

            Reward rolledReward = RollWeightedReward(validPool.Count > 0 ? validPool : rewardsPool);
            assignedSpotRewards.Add(rolledReward);

            if (rolledReward != null)
            {
                string key = !string.IsNullOrEmpty(rolledReward.rewardName) ? rolledReward.rewardName : rolledReward.value.ToString();
                if (rolledCounts.ContainsKey(key)) rolledCounts[key]++;
                else rolledCounts[key] = 1;

                if (i < rewardRenderers.Length && rewardRenderers[i] != null)
                {
                    rewardRenderers[i].sprite = rolledReward.rewardSprite;
                    rewardRenderers[i].enabled = true;
                }
            }
        }

        CalculateTotalWinnings();
    }

    public void InitializeSpotRewards(List<Reward> rewardsPool)
    {
        EnsureRewardRenderersExist();
        assignedSpotRewards.Clear();
        totalWinnings = 0;

        if (rewardsPool == null || rewardsPool.Count == 0) return;

        for (int i = 0; i < zones.Length; i++)
        {
            Reward rolledReward = RollWeightedReward(rewardsPool);
            assignedSpotRewards.Add(rolledReward);

            if (rolledReward != null && i < rewardRenderers.Length && rewardRenderers[i] != null)
            {
                rewardRenderers[i].sprite = rolledReward.rewardSprite;
                rewardRenderers[i].enabled = true;
            }
        }

        CalculateTotalWinnings();
    }

    public int CalculateRevealedWinnings()
    {
        totalWinnings = 0;
        if (assignedSpotRewards == null || assignedSpotRewards.Count == 0 || zones == null) return 0;

        List<Reward> revealedRewards = new List<Reward>();
        for (int i = 0; i < zones.Length && i < assignedSpotRewards.Count; i++)
        {
            if (zones[i] != null && zones[i].IsRevealed)
            {
                if (assignedSpotRewards[i] != null)
                {
                    revealedRewards.Add(assignedSpotRewards[i]);
                }
            }
        }

        if (revealedRewards.Count == 0) return 0;

        // Apple Tree Card: Sum all revealed positive/negative prizes
        if (appleTreeCardData != null)
        {
            foreach (var r in revealedRewards)
            {
                if (r != null) totalWinnings += r.value;
            }
            return totalWinnings;
        }

        // Quick Cash Card: Match 3
        if (quickCashCardData != null)
        {
            Dictionary<string, (Reward reward, int count)> quickCounts = new Dictionary<string, (Reward, int)>();
            foreach (var r in revealedRewards)
            {
                if (r == null) continue;
                string key = !string.IsNullOrEmpty(r.rewardName) ? r.rewardName : r.value.ToString();
                if (quickCounts.ContainsKey(key))
                {
                    var entry = quickCounts[key];
                    quickCounts[key] = (entry.reward, entry.count + 1);
                }
                else
                {
                    quickCounts[key] = (r, 1);
                }
            }

            foreach (var kvp in quickCounts.Values)
            {
                if (kvp.count >= 3)
                {
                    totalWinnings += kvp.reward.value;
                }
            }
            return totalWinnings;
        }

        // Star Scratch Card & Others (Match 2 to WIN, 3 for 2x)
        Dictionary<string, (Reward reward, int count)> rewardCounts = new Dictionary<string, (Reward, int)>();

        foreach (var r in revealedRewards)
        {
            if (r == null) continue;
            string key = !string.IsNullOrEmpty(r.rewardName) ? r.rewardName : r.value.ToString();
            if (rewardCounts.ContainsKey(key))
            {
                var entry = rewardCounts[key];
                rewardCounts[key] = (entry.reward, entry.count + 1);
            }
            else
            {
                rewardCounts[key] = (r, 1);
            }
        }

        foreach (var kvp in rewardCounts.Values)
        {
            if (kvp.count == 2)
            {
                totalWinnings += kvp.reward.value;
            }
            else if (kvp.count >= 3)
            {
                totalWinnings += kvp.reward.value * 2;
            }
        }

        return totalWinnings;
    }

    public int CalculateTotalWinnings()
    {
        return CalculateRevealedWinnings();
    }

    private Reward RollWeightedReward(List<Reward> rewardsPool)
    {
        // Delegate to UpgradeManager so the Scratch Luck level is respected.
        if (UpgradeManager.Instance != null)
            return UpgradeManager.Instance.RollWithLuck(rewardsPool);

        // ── Fallback (no UpgradeManager in scene) ──────────────────────
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
                return r;
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