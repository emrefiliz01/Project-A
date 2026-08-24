using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CardInfoPanelUI : MonoBehaviour
{
    public static CardInfoPanelUI Instance { get; private set; }

    [System.Serializable]
    public class PanelGroup
    {
        public GameObject panelRoot;
        public TMP_Text cardNameText;
        public TMP_Text cardDescriptionText;
        public TMP_Text symbolChanceHeaderText;
        public TMP_Text[] customRowTexts;
    }

    [Header("Panels Configuration")]
    [SerializeField] private PanelGroup mysteryCouponPanel;
    [SerializeField] private PanelGroup starCardPanel;
    [SerializeField] private PanelGroup appleTreeCardPanel;
    [SerializeField] private PanelGroup quickCashCardPanel;

    private void Awake()
    {
        Instance = this;
        HidePanel();
    }

    public void ShowPanelForCard(GameObject cardObj)
    {
        if (cardObj == null) return;

        HidePanel();

        MultiZoneScratchCard multiCard = cardObj.GetComponent<MultiZoneScratchCard>();
        RewardManager rm = cardObj.GetComponent<RewardManager>();

        if (multiCard != null && multiCard.QuickCashCardData != null)
        {
            if (quickCashCardPanel != null && quickCashCardPanel.panelRoot != null)
            {
                quickCashCardPanel.panelRoot.SetActive(true);
                PopulateGroup(quickCashCardPanel, multiCard.QuickCashCardData.cardName, multiCard.QuickCashCardData.cardDescription, multiCard.QuickCashCardData.possibleRewards, false);
            }
            return;
        }

        if (multiCard != null && multiCard.AppleTreeCardData != null)
        {
            if (appleTreeCardPanel != null && appleTreeCardPanel.panelRoot != null)
            {
                appleTreeCardPanel.panelRoot.SetActive(true);
                PopulateGroup(appleTreeCardPanel, multiCard.AppleTreeCardData.cardName, multiCard.AppleTreeCardData.cardDescription, multiCard.AppleTreeCardData.possibleRewards, true);
            }
            return;
        }

        if (multiCard != null && (multiCard.StarCardData != null || multiCard.DefaultCardData != null))
        {
            if (starCardPanel != null && starCardPanel.panelRoot != null)
            {
                starCardPanel.panelRoot.SetActive(true);
                var starData = multiCard.StarCardData;
                if (starData != null)
                {
                    PopulateGroup(starCardPanel, starData.cardName, starData.cardDescription, starData.possibleRewards, false);
                }
                else if (multiCard.DefaultCardData != null)
                {
                    PopulateGroup(starCardPanel, multiCard.DefaultCardData.cardName, multiCard.DefaultCardData.cardDescription, multiCard.DefaultCardData.rewardsList, false);
                }
            }
            return;
        }

        // 4. MYSTERY COUPON KONTROLÜ
        if (rm != null && rm.CardData != null)
        {
            if (mysteryCouponPanel != null && mysteryCouponPanel.panelRoot != null)
            {
                mysteryCouponPanel.panelRoot.SetActive(true);
                PopulateGroup(mysteryCouponPanel, rm.CardData.cardName, rm.CardData.cardDescription, rm.CardData.rewardsList, false);
            }
            return;
        }
    }

    public void HidePanel()
    {
        if (mysteryCouponPanel != null && mysteryCouponPanel.panelRoot != null)
            mysteryCouponPanel.panelRoot.SetActive(false);

        if (starCardPanel != null && starCardPanel.panelRoot != null)
            starCardPanel.panelRoot.SetActive(false);

        if (appleTreeCardPanel != null && appleTreeCardPanel.panelRoot != null)
            appleTreeCardPanel.panelRoot.SetActive(false);

        if (quickCashCardPanel != null && quickCashCardPanel.panelRoot != null)
            quickCashCardPanel.panelRoot.SetActive(false);
    }

    private void PopulateGroup(PanelGroup group, string cardName, string cardDescription, List<Reward> rewards, bool isAppleTree)
    {
        if (group == null) return;

        if (group.cardNameText != null)
            group.cardNameText.text = !string.IsNullOrEmpty(cardName) ? cardName : "Scratch Card";

        if (group.cardDescriptionText != null)
            group.cardDescriptionText.text = !string.IsNullOrEmpty(cardDescription) ? cardDescription : "";

        if (group.symbolChanceHeaderText != null)
            group.symbolChanceHeaderText.text = "Symbol chance:";

        if (group.customRowTexts != null)
        {
            foreach (var t in group.customRowTexts)
                if (t != null) t.text = "";
        }

        if (rewards == null || rewards.Count == 0) return;

        int totalWeight = 0;
        foreach (var r in rewards)
            if (r != null) totalWeight += Mathf.Max(1, r.weight);

        for (int i = 0; i < rewards.Count; i++)
        {
            Reward r = rewards[i];
            if (r == null) continue;

            int weight = Mathf.Max(1, r.weight);
            int percent = totalWeight > 0 ? Mathf.RoundToInt((float)weight / totalWeight * 100f) : 0;

            string valueDisplay = isAppleTree ? (r.value >= 0 ? $"${r.value}" : $"-${Mathf.Abs(r.value)}") : $"${r.value}";
            string rowString = $"{percent}%\t\t{valueDisplay}";

            if (group.customRowTexts != null && i < group.customRowTexts.Length && group.customRowTexts[i] != null)
            {
                group.customRowTexts[i].text = rowString;
            }
        }
    }
}