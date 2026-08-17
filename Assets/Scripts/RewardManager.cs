using UnityEngine;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class Reward
{
    public string rewardName;
    public int value;
    public Sprite rewardSprite;
    public string rewardText;
    public int weight;
}

public class RewardManager : MonoBehaviour
{
    [SerializeField] private ScratchCardData cardData;
    [SerializeField] private List<Reward> rewardsList = new List<Reward>();
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private TextMeshPro targetTextComponent;

    private Reward activeReward;
    private int totalWinnings = 0;

    public int TotalWinnings => totalWinnings;
    public Reward ActiveReward => activeReward;
    public int ActiveRewardValue => activeReward != null ? activeReward.value : 0;

    private void Start()
    {
        SetupNewCard();
    }

    public void Initialize(ScratchCardData data)
    {
        cardData = data;

        if (cardData != null && cardData.rewardsList != null && cardData.rewardsList.Count > 0)
        {
            rewardsList = cardData.rewardsList;
        }

        SetupNewCard();
    }

    public void SetupNewCard()
    {
        if (rewardsList == null || rewardsList.Count == 0)
        {
            Debug.LogWarning("Rewards list is empty!");
            return;
        }

        int totalWeight = 0;
        foreach (Reward reward in rewardsList)
        {
            totalWeight += reward.weight;
        }

        int randomValue = Random.Range(0, totalWeight);


        activeReward = null;
        int currentSum = 0;
        foreach (Reward reward in rewardsList)
        {
            currentSum += reward.weight;
            if (randomValue < currentSum)
            {
                activeReward = reward;
                break;
            }
        }

        if (activeReward != null)
        {
            Debug.Log($"[LootTable] Rolled value: {randomValue} / Total weight: {totalWeight}. Selected: '{activeReward.rewardName}' (Weight: {activeReward.weight})");

            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.sprite = activeReward.rewardSprite;
            }

            if (targetTextComponent != null)
            {
                targetTextComponent.text = activeReward.rewardText;
            }
        }
    }

    public void ClaimReward()
    {
        if (activeReward != null)
        {
            totalWinnings += activeReward.value;
            Debug.Log("Claimed: " + activeReward.rewardName + ". Total Winnings: $" + totalWinnings);
            activeReward = null;
        }
    }
}
