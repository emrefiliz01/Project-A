using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central singleton that owns the Scratch Size and Scratch Luck upgrades.
///
/// Wire-up in the Inspector:
///   • scratchSizeDefinition  → the UpgradeDefinition asset for Scratch Size
///   • scratchLuckDefinition  → the UpgradeDefinition asset for Scratch Luck
///   • baseRadius             → starting brush radius (should match ScratchCard.brushRadius default, i.e. 20)
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    // ─────────────────────────── Singleton ────────────────────────────
    public static UpgradeManager Instance { get; private set; }

    // ─────────────────────────── Inspector ────────────────────────────
    [Header("Upgrade Definitions")]
    [SerializeField] private UpgradeDefinition scratchSizeDefinition;
    [SerializeField] private UpgradeDefinition scratchLuckDefinition;

    [Header("Brush Radius")]
    [Tooltip("Base radius before any Scratch Size upgrades (should match ScratchCard.brushRadius default).")]
    [SerializeField] private int baseRadius = 20;

    // ─────────────────────────── State ────────────────────────────────
    private int scratchSizeLevel = 0;
    private int scratchLuckLevel = 0;

    // ─────────────────────────── Events ───────────────────────────────
    /// <summary>Fired after a Scratch Size purchase. Passes the new level.</summary>
    public event Action<int> OnSizeLevelChanged;

    /// <summary>Fired after a Scratch Luck purchase. Passes the new level.</summary>
    public event Action<int> OnLuckLevelChanged;

    // ─────────────────────────── Public read-only props ───────────────
    public int ScratchSizeLevel => scratchSizeLevel;
    public int ScratchLuckLevel => scratchLuckLevel;

    public int CurrentBrushRadius =>
        baseRadius + scratchSizeLevel * (scratchSizeDefinition != null ? scratchSizeDefinition.brushRadiusPerLevel : 5);

    public int NextSizeCost =>
        scratchSizeDefinition != null ? scratchSizeDefinition.GetCostForLevel(scratchSizeLevel) : 0;

    public int NextLuckCost =>
        scratchLuckDefinition != null ? scratchLuckDefinition.GetCostForLevel(scratchLuckLevel) : 0;

    public bool SizeAtMaxLevel =>
        scratchSizeDefinition != null && scratchSizeDefinition.maxLevel > 0
        && scratchSizeLevel >= scratchSizeDefinition.maxLevel;

    public bool LuckAtMaxLevel =>
        scratchLuckDefinition != null && scratchLuckDefinition.maxLevel > 0
        && scratchLuckLevel >= scratchLuckDefinition.maxLevel;

    /// <summary>Read-only access to the Scratch Size definition asset (used by UpgradeUI).</summary>
    public UpgradeDefinition ScratchSizeDefinition => scratchSizeDefinition;

    /// <summary>Read-only access to the Scratch Luck definition asset (used by UpgradeUI).</summary>
    public UpgradeDefinition ScratchLuckDefinition => scratchLuckDefinition;

    // ──────────────────────────── Unity ───────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ───────────────────────── Purchase API ───────────────────────────

    /// <summary>
    /// Attempts to purchase one level of the Scratch Size upgrade.
    /// Returns true if the purchase succeeded.
    /// </summary>
    public bool TryPurchaseScratchSize()
    {
        if (scratchSizeDefinition == null)
        {
            Debug.LogWarning("[UpgradeManager] scratchSizeDefinition is not assigned.");
            return false;
        }
        if (SizeAtMaxLevel)
        {
            Debug.Log("[UpgradeManager] Scratch Size is already at max level.");
            return false;
        }

        int cost = NextSizeCost;
        if (GameManager.Instance == null || GameManager.Instance.PlayerMoney < cost)
            return false;

        GameManager.Instance.SpendMoney(cost);
        scratchSizeLevel++;

        ApplyBrushRadiusToAllCards();
        OnSizeLevelChanged?.Invoke(scratchSizeLevel);

        Debug.Log($"[UpgradeManager] Scratch Size → Level {scratchSizeLevel}. Brush radius = {CurrentBrushRadius}");
        return true;
    }

    /// <summary>
    /// Attempts to purchase one level of the Scratch Luck upgrade.
    /// Returns true if the purchase succeeded.
    /// </summary>
    public bool TryPurchaseScratchLuck()
    {
        if (scratchLuckDefinition == null)
        {
            Debug.LogWarning("[UpgradeManager] scratchLuckDefinition is not assigned.");
            return false;
        }
        if (LuckAtMaxLevel)
        {
            Debug.Log("[UpgradeManager] Scratch Luck is already at max level.");
            return false;
        }

        int cost = NextLuckCost;
        if (GameManager.Instance == null || GameManager.Instance.PlayerMoney < cost)
            return false;

        GameManager.Instance.SpendMoney(cost);
        scratchLuckLevel++;

        OnLuckLevelChanged?.Invoke(scratchLuckLevel);

        Debug.Log($"[UpgradeManager] Scratch Luck → Level {scratchLuckLevel}.");
        return true;
    }

    // ─────────────────────── Scratch Size Logic ───────────────────────

    /// <summary>
    /// Finds every active ScratchCard / ScratchZone in the scene and updates brushRadius.
    /// Called automatically on purchase; also safe to call manually.
    /// </summary>
    public void ApplyBrushRadiusToAllCards()
    {
        int radius = CurrentBrushRadius;
        ScratchCard[] allCards = FindObjectsByType<ScratchCard>(FindObjectsSortMode.None);
        foreach (var card in allCards)
        {
            if (card != null)
                card.brushRadius = radius;
        }
    }

    // ──────────────────────── Scratch Luck Logic ──────────────────────

    /// <summary>
    /// Returns a list of luck-adjusted effective weights for the supplied reward pool,
    /// taking the current Scratch Luck level into account.
    ///
    /// Formula per reward:
    ///   luckBonus    = luckBonusPerLevel * luckLevel
    ///   rarityFactor = totalWeight / Mathf.Max(1, r.weight)   ← rarer items boosted more
    ///   boostedWeight = r.weight + RoundToInt(r.weight * luckBonus * rarityFactor)
    ///
    /// The returned list maps 1-to-1 with <paramref name="rewards"/> (same indices).
    /// Original ScriptableObject data is never mutated.
    /// </summary>
    public List<int> GetModifiedWeights(List<Reward> rewards)
    {
        var result = new List<int>(rewards.Count);

        if (scratchLuckLevel == 0 || scratchLuckDefinition == null)
        {
            // No luck → return raw weights unchanged.
            foreach (var r in rewards)
                result.Add(r != null ? Mathf.Max(1, r.weight) : 1);
            return result;
        }

        // 1. Gather all unique reward values and sort them ascending to determine reward tiers.
        var uniqueValues = new List<int>();
        foreach (var r in rewards)
        {
            if (r != null && !uniqueValues.Contains(r.value))
                uniqueValues.Add(r.value);
        }
        uniqueValues.Sort();

        int tierCount = uniqueValues.Count;
        float luckFactor = 1f + (scratchLuckDefinition.luckBonusPerLevel * scratchLuckLevel);

        // 2. Scale each reward's weight exponentially based on its value tier (0.0 to 1.0).
        foreach (var r in rewards)
        {
            if (r == null)
            {
                result.Add(1);
                continue;
            }

            int baseW = Mathf.Max(1, r.weight);

            // Normalized rank S in [0.0, 1.0]: 0.0 = lowest prize/penalty, 1.0 = highest jackpot
            float normalizedTier = 0.5f;
            if (tierCount > 1)
            {
                int rank = uniqueValues.IndexOf(r.value);
                normalizedTier = (float)rank / (tierCount - 1);
            }

            // Exponential tier multiplier: top prizes scale aggressively with Luck Level,
            // while low-tier prizes stay small so their relative percentage drops significantly.
            float tierMultiplier = Mathf.Pow(luckFactor, normalizedTier * 3.0f);

            int boostedWeight = Mathf.Max(1, Mathf.RoundToInt(baseW * tierMultiplier));
            result.Add(boostedWeight);
        }

        return result;
    }

    /// <summary>
    /// Convenience helper — rolls a weighted reward from a pool using luck-adjusted weights.
    /// Useful so callers don't have to combine GetModifiedWeights + roll themselves.
    /// </summary>
    public Reward RollWithLuck(List<Reward> rewardsPool)
    {
        if (rewardsPool == null || rewardsPool.Count == 0) return null;

        List<int> weights = GetModifiedWeights(rewardsPool);

        int totalWeight = 0;
        foreach (int w in weights) totalWeight += w;

        if (totalWeight <= 0) return rewardsPool[0];

        int rnd = UnityEngine.Random.Range(0, totalWeight);
        int currentSum = 0;

        for (int i = 0; i < rewardsPool.Count; i++)
        {
            if (rewardsPool[i] == null) continue;
            currentSum += weights[i];
            if (rnd < currentSum)
                return rewardsPool[i];
        }

        return rewardsPool[0];
    }
}
