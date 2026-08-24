using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "QuickCashCardData", menuName = "Quick Cash Card Data")]
public class QuickCashCardScriptableObject : ScriptableObject
{
    public string cardName = "Quick Cash Card";
    public string cardDescription = "Match 3 to WIN";
    public int purchasePrice = 5000;

    [Header("Sprites")]
    public Sprite cardBaseSprite;
    public Sprite zoneCoverSprite;
    public Sprite bonusZoneCoverSprite;

    public List<Reward> possibleRewards = new List<Reward>();

    [Range(0.1f, 1.0f)]
    public float zoneRevealThreshold = 0.90f;

    [Range(0.1f, 1.0f)]
    public float overallCardThreshold = 1.0f;
}