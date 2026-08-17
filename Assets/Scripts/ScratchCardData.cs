using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewScratchCard", menuName = "Scratch Card/Card Data")]
public class ScratchCardData : ScriptableObject
{
    public string cardName;
    public int purchasePrice = 10;
    public Sprite coverSprite;
    public Sprite cardBaseSprite;
    public List<Reward> rewardsList = new List<Reward>();
}
