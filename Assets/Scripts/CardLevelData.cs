using System;

/// <summary>
/// Tracks Level (0–10) and EXP for a single card type.
/// EXP formula: RequiredEXP = (CurrentLevel + 1) * 4
/// Reward scaling formula: Round(baseValue * (1 + Level * 0.30))
/// </summary>
[System.Serializable]
public class CardLevelData
{
    public const int MaxLevel = 10;

    private int level = 0;
    private int currentEXP = 0;

    public int Level => level;
    public int CurrentEXP => currentEXP;
    public bool IsMaxLevel => level >= MaxLevel;

    /// <summary>
    /// EXP cards required to reach the next level.
    /// Formula: (CurrentLevel + 1) * 4. Returns 0 at MAX level.
    /// </summary>
    public int RequiredEXP => IsMaxLevel ? 0 : (level + 1) * 4;

    /// <summary>
    /// Adds exactly 1 EXP (one collected card).
    /// Handles level-up automatically.
    /// Returns true if a level-up occurred.
    /// </summary>
    public bool AddEXP()
    {
        if (IsMaxLevel) return false;

        currentEXP++;

        if (currentEXP >= RequiredEXP)
        {
            currentEXP = 0;
            level++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Scales a base reward value by the current card level.
    /// Formula: Round(baseValue * (1 + Level * 0.30))
    /// Level 0 returns the base value unchanged.
    /// </summary>
    public int ScaleReward(int baseValue)
    {
        if (level == 0) return baseValue;
        return (int)Math.Round(baseValue * (1.0 + level * 0.30));
    }
}
