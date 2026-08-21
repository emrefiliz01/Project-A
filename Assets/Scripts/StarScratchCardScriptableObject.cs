using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StarScratchCardData", menuName = "Scratch Card/Star Scratch Card Data")]
public class StarScratchCardScriptableObject : ScriptableObject
{
    [Header("Card Info")]
    public string cardName = "Star Scratch Card";
    public string cardDescription = "Match 2 to WIN";
    public int purchasePrice = 20;

    [Header("Sprites")]
    public Sprite cardBaseSprite;
    public Sprite zoneCoverSprite;

    [Header("Rewards Pool (e.g. CoinBag, Dollar, Coins)")]
    public List<Reward> possibleRewards = new List<Reward>();

    [Header("Per-Zone & Completion Settings")]
    [Range(0.1f, 1.0f)]
    public float zoneRevealThreshold = 0.80f;

    [Range(0.1f, 1.0f)]
    public float overallCardThreshold = 1.0f;
}
