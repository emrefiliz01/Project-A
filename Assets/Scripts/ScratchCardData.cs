using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewScratchCard", menuName = "Scratch Card/Card Data")]
public class ScratchCardData : ScriptableObject
{
    public string cardName;
    public int purchasePrice = 10;
    public Sprite coverSprite;
    public Sprite cardBaseSprite;

    [Range(0.1f, 1.0f)]
    public float scratchThreshold = 0.90f;

    public List<Reward> rewardsList = new List<Reward>();
}
