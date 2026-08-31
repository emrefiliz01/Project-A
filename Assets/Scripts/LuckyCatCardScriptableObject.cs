using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LuckyCatCardData", menuName = "Scratch Card/Lucky Cat Card Data")]
public class LuckyCatCardScriptableObject : ScriptableObject
{
    public string cardName = "Lucky Cat";
    public string cardDescription = "Watch out for fish bones!";
    public int purchasePrice = 1000;

    [Header("Sprites")]
    public Sprite cardBaseSprite;
    public Sprite zoneCoverSprite;
    public Sprite specialZoneCoverSprite;

    public List<Reward> possibleRewards = new List<Reward>();

    [Range(0.1f, 1.0f)]
    public float zoneRevealThreshold = 0.80f;

    [Range(0.1f, 1.0f)]
    public float overallCardThreshold = 1.0f;
}