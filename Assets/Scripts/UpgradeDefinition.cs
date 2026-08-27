using UnityEngine;

/// <summary>
/// ScriptableObject asset that holds the editable configuration for one upgrade slot.
/// Create two assets via the Unity Asset menu: one for Scratch Size, one for Scratch Luck.
/// </summary>
[CreateAssetMenu(fileName = "NewUpgradeDefinition", menuName = "Scratch Card/Upgrade Definition")]
public class UpgradeDefinition : ScriptableObject
{
    [Header("Display")]
    public string upgradeName = "Upgrade";

    [Tooltip("Icon shown in the upgrade button container.")]
    public Sprite upgradeIcon;

    [Header("Cost Scaling")]
    [Tooltip("Cost at level 0 → 1.")]
    public int baseCost = 100;

    [Tooltip("Each level multiplies the previous cost by this factor.\n" +
             "Formula: Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel))")]
    public float costMultiplier = 1.8f;

    [Header("Level Cap")]
    [Tooltip("Maximum level the player can reach. Set to 0 for unlimited.")]
    public int maxLevel = 10;

    [Header("Scratch Size (leave 0 for Luck upgrade)")]
    [Tooltip("Pixels added to brushRadius per level.")]
    public int brushRadiusPerLevel = 5;

    [Header("Scratch Luck (leave 0 for Size upgrade)")]
    [Tooltip("Luck scaling strength per level.\n" +
             "Recommended: 0.35 to 0.50 for strong, rewarding jackpot progression.")]
    public float luckBonusPerLevel = 0.35f;

    /// <summary>Returns the purchase cost for upgrading FROM the given current level.</summary>
    public int GetCostForLevel(int currentLevel)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
    }
}
