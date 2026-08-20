using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CardInfoPanelUI : MonoBehaviour
{
    public static CardInfoPanelUI Instance { get; private set; }

    [Header("Main Text References")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text cardDescriptionText;
    [SerializeField] private TMP_Text symbolChanceHeaderText;

    [Header("Per-Row Texts (next to DollarImage, CoinsImage, CoinBagImage)")]
    [Tooltip("Assign TextMeshPro objects in order (e.g. DollarInfo, CoinsInfo, CoinBagInfo).")]
    [SerializeField] private TMP_Text[] customRowTexts;

    [Header("Named Row Text Fields (Optional)")]
    [SerializeField] private TMP_Text dollarChanceText;
    [SerializeField] private TMP_Text coinsChanceText;
    [SerializeField] private TMP_Text coinBagChanceText;

    private void Awake()
    {
        Instance = this;
        FindReferencesIfMissing();
        gameObject.SetActive(false);
    }

    public void ShowPanelForCard(GameObject cardObj)
    {
        gameObject.SetActive(true);
        UpdateFromCard(cardObj);
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }

    public void Initialize(StarScratchCardScriptableObject data)
    {
        if (data == null) return;
        Populate(data.cardName, data.cardDescription, data.possibleRewards);
    }

    public void Initialize(ScratchCardData data)
    {
        if (data == null) return;
        Populate(data.cardName, data.cardDescription, data.rewardsList);
    }

    public void UpdateFromCard(GameObject cardObj)
    {
        if (cardObj == null) return;

        MultiZoneScratchCard multiCard = cardObj.GetComponent<MultiZoneScratchCard>();
        if (multiCard != null)
        {
            if (multiCard.StarCardData != null)
            {
                Initialize(multiCard.StarCardData);
                return;
            }
            else if (multiCard.DefaultCardData != null)
            {
                Initialize(multiCard.DefaultCardData);
                return;
            }
        }

        RewardManager rm = cardObj.GetComponent<RewardManager>();
        if (rm != null && rm.CardData != null)
        {
            Initialize(rm.CardData);
        }
    }

    private void FindReferencesIfMissing()
    {
        if (cardNameText == null)
        {
            Transform t = transform.Find("CardName");
            if (t != null) cardNameText = t.GetComponent<TMP_Text>();
        }

        if (cardDescriptionText == null)
        {
            Transform t = transform.Find("CardDescription");
            if (t != null) cardDescriptionText = t.GetComponent<TMP_Text>();
        }

        if (symbolChanceHeaderText == null)
        {
            Transform t = transform.Find("SymbolChance");
            if (t != null) symbolChanceHeaderText = t.GetComponent<TMP_Text>();
        }
    }

    private void Populate(string cardName, string cardDescription, List<Reward> rewards)
    {
        FindReferencesIfMissing();

        if (cardNameText != null)
            cardNameText.text = !string.IsNullOrEmpty(cardName) ? cardName : "Star Scratch Card";

        if (cardDescriptionText != null)
            cardDescriptionText.text = !string.IsNullOrEmpty(cardDescription) ? cardDescription : "Match 2 to WIN";

        if (symbolChanceHeaderText != null)
            symbolChanceHeaderText.text = "Symbol chance";

        if (customRowTexts != null)
        {
            foreach (var t in customRowTexts)
                if (t != null) t.text = "";
        }
        if (dollarChanceText != null) dollarChanceText.text = "";
        if (coinsChanceText != null) coinsChanceText.text = "";
        if (coinBagChanceText != null) coinBagChanceText.text = "";

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
            string rowString = $"{percent}%\t\t${r.value}";

            if (customRowTexts != null && i < customRowTexts.Length && customRowTexts[i] != null)
            {
                customRowTexts[i].text = rowString;
            }

            if (!string.IsNullOrEmpty(r.rewardName))
            {
                string lower = r.rewardName.ToLower();
                if (lower.Contains("dollar") && dollarChanceText != null)
                    dollarChanceText.text = rowString;
                else if (lower.Contains("coinbag") && coinBagChanceText != null)
                    coinBagChanceText.text = rowString;
                else if (lower.Contains("coin") && coinsChanceText != null)
                    coinsChanceText.text = rowString;
            }
        }
    }
}
