using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewScratchCard", menuName = "Scratch Card/Card Data")]
public class ScratchCardData : ScriptableObject
{
    public string cardName;
    public string cardDescription = "Match 2 to WIN";
    public int purchasePrice = 10;
    public Sprite coverSprite;
    public Sprite cardBaseSprite;

    [Range(0.1f, 1.0f)]
    public float scratchThreshold = 0.90f;

    [Header("Localized Reward Symbol Settings")]
    public bool useLocalizedRewardCheck = false;
    public Rect rewardSymbolBounds = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
    [Range(0.1f, 1.0f)]
    public float symbolZoneThreshold = 0.85f;

    public List<Reward> rewardsList = new List<Reward>();
}

