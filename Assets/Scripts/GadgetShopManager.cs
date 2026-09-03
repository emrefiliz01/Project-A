using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

public class GadgetShopManager : MonoBehaviour
{
    public static GadgetShopManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject ticketsPanel;
    [SerializeField] private GameObject gadgetsPanel;

    [Header("Optional Canvas / Extra Panels")]
    [Tooltip("Tickets paneline ait dışarıdaki Canvas veya Level barlarını tutan obje")]
    [SerializeField] private GameObject ticketsCanvas;

    [Header("Tab Buttons & Visuals")]
    [SerializeField] private GameObject ticketsTabButton;
    [SerializeField] private GameObject gadgetsTabButton;
    [SerializeField] private SpriteRenderer ticketsTabSprite;
    [SerializeField] private SpriteRenderer gadgetsTabSprite;
    [SerializeField] private Color activeTabColor = new Color(0.2f, 0.6f, 1f, 1f); // Mavi
    [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.6f, 0.6f, 1f); // Gri

    [Header("Robot Purchase Settings")]
    [SerializeField] private GameObject buyRobotButton;
    [SerializeField] private TextMeshPro robotPriceText;
    [SerializeField] private GameObject scratchRobotInScene; // Sahnemizdeki Robot objesi
    [SerializeField] private Transform robotSpawnPoint;       // Robotun doğacağı nokta
    [SerializeField] private int baseRobotPrice = 50000;

    [Header("Capacity Upgrade Settings")]
    [SerializeField] private GameObject upgradeCapacityButton;
    [SerializeField] private TextMeshPro capacityPriceText;
    [SerializeField] private TextMeshPro capacityLevelText;
    [SerializeField] private int baseCapacityPrice = 50000;

    [Header("Speed Upgrade Settings")]
    [SerializeField] private GameObject upgradeSpeedButton;
    [SerializeField] private TextMeshPro speedPriceText;
    [SerializeField] private TextMeshPro speedLevelText;
    [SerializeField] private int baseSpeedPrice = 50000;

    [Header("Visual Colors & Text Dimming")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.red;
    [SerializeField] private Color maxedColor = Color.yellow;
    [SerializeField] private Color lockedDimColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Koyu Gri (Yazılar için)

    // State Variables
    private bool isRobotOwned = false;

    // Capacity Logic: Base 4, Max 32 (+4 per level => Max Lvl 8)
    private int capacityLevel = 1;
    private const int maxCapacityLevel = 8;
    private const int baseCapacityValue = 4;

    // Speed Logic: Base 8s, Max Lvl 5 (Durations: 8s -> 6.5s -> 5s -> 3.5s -> 2s)
    private int speedLevel = 1;
    private const int maxSpeedLevel = 5;
    private readonly float[] speedDurations = new float[] { 8.0f, 6.5f, 5.0f, 3.5f, 2.0f };

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Başlangıçta Robot Gizli
        if (scratchRobotInScene != null)
        {
            scratchRobotInScene.SetActive(false);
        }

        // Başlangıçta Tickets Paneli Açık
        SelectTicketsTab();
        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

            if (hit.collider != null)
            {
                GameObject clickedObj = hit.collider.gameObject;

                if (clickedObj == ticketsTabButton || clickedObj.name == "TicketsPanelText")
                {
                    SelectTicketsTab();
                }
                else if (clickedObj == gadgetsTabButton || clickedObj.name == "GadgetsPanelText")
                {
                    SelectGadgetsTab();
                }
                else if (clickedObj == buyRobotButton)
                {
                    TryBuyRobot();
                }
                else if (clickedObj == upgradeCapacityButton)
                {
                    TryUpgradeCapacity();
                }
                else if (clickedObj == upgradeSpeedButton)
                {
                    TryUpgradeSpeed();
                }
            }
        }

        UpdateUIColors();
    }

    #region Tab Switching
    public void SelectTicketsTab()
    {
        if (ticketsPanel != null) ticketsPanel.SetActive(true);
        if (ticketsCanvas != null) ticketsCanvas.SetActive(true);
        if (gadgetsPanel != null) gadgetsPanel.SetActive(false);

        if (ticketsTabSprite != null) ticketsTabSprite.color = activeTabColor;
        if (gadgetsTabSprite != null) gadgetsTabSprite.color = inactiveTabColor;
    }

    public void SelectGadgetsTab()
    {
        if (ticketsPanel != null) ticketsPanel.SetActive(false);
        if (ticketsCanvas != null) ticketsCanvas.SetActive(false);
        if (gadgetsPanel != null) gadgetsPanel.SetActive(true);

        if (ticketsTabSprite != null) ticketsTabSprite.color = inactiveTabColor;
        if (gadgetsTabSprite != null) gadgetsTabSprite.color = activeTabColor;
    }
    #endregion

    #region Robot Purchase & Upgrades
    private void TryBuyRobot()
    {
        if (isRobotOwned) return;

        if (GameManager.Instance != null && GameManager.Instance.PlayerMoney >= baseRobotPrice)
        {
            GameManager.Instance.SpendMoney(baseRobotPrice);
            isRobotOwned = true;

            if (scratchRobotInScene != null)
            {
                if (robotSpawnPoint != null)
                {
                    scratchRobotInScene.transform.position = robotSpawnPoint.position;
                }
                scratchRobotInScene.SetActive(true);

                ApplyCapacityToRobot();
                ApplySpeedToRobot();
            }

            UpdateUI();
        }
    }

    private void TryUpgradeCapacity()
    {
        if (!isRobotOwned || capacityLevel >= maxCapacityLevel) return;

        int cost = GetCapacityUpgradePrice();
        if (GameManager.Instance != null && GameManager.Instance.PlayerMoney >= cost)
        {
            GameManager.Instance.SpendMoney(cost);
            capacityLevel++;

            ApplyCapacityToRobot();
            UpdateUI();
        }
    }

    private void TryUpgradeSpeed()
    {
        if (!isRobotOwned || speedLevel >= maxSpeedLevel) return;

        int cost = GetSpeedUpgradePrice();
        if (GameManager.Instance != null && GameManager.Instance.PlayerMoney >= cost)
        {
            GameManager.Instance.SpendMoney(cost);
            speedLevel++;

            ApplySpeedToRobot();
            UpdateUI();
        }
    }

    private void ApplyCapacityToRobot()
    {
        if (ScratchRobot.Instance != null)
        {
            int currentCapacity = baseCapacityValue + ((capacityLevel - 1) * 4);
            ScratchRobot.Instance.SetMaxCapacity(currentCapacity);
        }
    }

    private void ApplySpeedToRobot()
    {
        if (ScratchRobot.Instance != null)
        {
            float duration = speedDurations[Mathf.Clamp(speedLevel - 1, 0, speedDurations.Length - 1)];
            ScratchRobot.Instance.SetProcessDuration(duration);
        }
    }

    public int GetCapacityUpgradePrice()
    {
        return baseCapacityPrice * (int)Mathf.Pow(3, capacityLevel - 1);
    }

    public int GetSpeedUpgradePrice()
    {
        return baseSpeedPrice * (int)Mathf.Pow(3, speedLevel - 1);
    }
    #endregion

    #region Formatting & UI Updates
    /// <summary>
    /// Çift $ işaretini önler ve düzgün formatlama sağlar (örn: "$ 50.000", "$ 50")
    /// </summary>
    private string FormatPriceText(int amount)
    {
        string formatted = CurrencyFormatter.FormatMoney(amount);
        if (formatted.StartsWith("$"))
        {
            formatted = formatted.Substring(1).Trim();
        }
        return "$ " + formatted;
    }

    private void UpdateUI()
    {
        // 1. Robot Satın Alma Butonu
        if (robotPriceText != null)
        {
            if (isRobotOwned)
            {
                robotPriceText.text = "Owned";
            }
            else
            {
                robotPriceText.text = FormatPriceText(baseRobotPrice);
            }
        }

        // 2. Kapasite Upgrade UI
        if (capacityLevelText != null)
        {
            capacityLevelText.text = "Lvl " + capacityLevel;
        }

        if (capacityPriceText != null)
        {
            if (capacityLevel >= maxCapacityLevel)
            {
                capacityPriceText.text = "Maxed";
            }
            else
            {
                capacityPriceText.text = FormatPriceText(GetCapacityUpgradePrice());
            }
        }

        // 3. Hız Upgrade UI
        if (speedLevelText != null)
        {
            speedLevelText.text = "Lvl " + speedLevel;
        }

        if (speedPriceText != null)
        {
            if (speedLevel >= maxSpeedLevel)
            {
                speedPriceText.text = "Maxed";
            }
            else
            {
                speedPriceText.text = FormatPriceText(GetSpeedUpgradePrice());
            }
        }
    }

    private void UpdateUIColors()
    {
        int playerMoney = GameManager.Instance != null ? GameManager.Instance.PlayerMoney : 0;

        // Robot Satın Alma Fiyat Rengi
        if (robotPriceText != null)
        {
            if (isRobotOwned)
            {
                robotPriceText.color = affordableColor;
            }
            else
            {
                robotPriceText.color = (playerMoney >= baseRobotPrice) ? affordableColor : unaffordableColor;
            }
        }

        // SADECE Fiyat ve Seviye Yazılarının Renklerini Değiştiriyoruz (Panel Sprite'larına Dokunulmaz)
        SetUpgradeTextVisuals(capacityPriceText, capacityLevelText, isRobotOwned, capacityLevel >= maxCapacityLevel, GetCapacityUpgradePrice(), playerMoney);
        SetUpgradeTextVisuals(speedPriceText, speedLevelText, isRobotOwned, speedLevel >= maxSpeedLevel, GetSpeedUpgradePrice(), playerMoney);
    }

    private void SetUpgradeTextVisuals(TextMeshPro priceText, TextMeshPro levelText, bool unlocked, bool isMaxed, int price, int playerMoney)
    {
        if (!unlocked)
        {
            // Robot henüz satın alınmadıysa SADECE YAZILARI karartıyoruz
            if (priceText != null) priceText.color = lockedDimColor;
            if (levelText != null) levelText.color = lockedDimColor;
        }
        else
        {
            // Robot alındıysa seviye metni normal renge döner
            if (levelText != null) levelText.color = Color.white;

            if (priceText != null)
            {
                if (isMaxed)
                {
                    priceText.color = maxedColor; // Sarı "Maxed"
                }
                else
                {
                    priceText.color = (playerMoney >= price) ? affordableColor : unaffordableColor;
                }
            }
        }
    }
    #endregion
}