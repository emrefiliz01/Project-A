using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AppleTreeCardData", menuName = "Apple Tree Card Data")]
public class AppleTreeCardScriptableObject : ScriptableObject
{
    public string cardName = "Apple Tree Card";
    public string cardDescription = "Find Apples and win $";
    public int purchasePrice = 500;

    [Header("Sprites")]
    public Sprite cardBaseSprite;
    public Sprite zoneCoverSprite;

    public List<Reward> possibleRewards = new List<Reward>();

    [Range(0.1f, 1.0f)]
    public float zoneRevealThreshold = 0.90f;

    [Range(0.1f, 1.0f)]
    public float overallCardThreshold = 1.0f;
}
