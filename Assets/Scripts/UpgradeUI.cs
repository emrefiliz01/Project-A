using UnityEngine;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    // ─────────────────────────── Inner type ───────────────────────────
    [System.Serializable]
    public class UpgradeSlotUI
    {
        [Tooltip("The world-space GameObject the player clicks to buy this upgrade.\n" +
                 "Must have a Collider2D attached (e.g. BoxCollider2D).")]
        public GameObject clickTarget;

        [Tooltip("(Optional) TextMeshPro component that shows the upgrade name.")]
        public TMP_Text nameText;

        [Tooltip("TextMeshPro component that shows the next-level cost.\n" +
                 "Turns red when the player cannot afford it.")]
        public TMP_Text costText;

        [Tooltip("TextMeshPro component that shows the current level.")]
        public TMP_Text levelText;

        [Tooltip("(Optional) SpriteRenderer that shows the upgrade icon.")]
        public SpriteRenderer iconRenderer;
    }

    // ─────────────────────────── Inspector ────────────────────────────
    [Header("Upgrade Slots")]
    [SerializeField] private UpgradeSlotUI scratchSizeSlot;
    [SerializeField] private UpgradeSlotUI scratchLuckSlot;

    [Header("Colour Settings")]
    [SerializeField] private Color affordableColor   = Color.white;
    [SerializeField] private Color unaffordableColor = Color.red;
    [SerializeField] private Color maxLevelColor     = new Color(1f, 0.84f, 0f); // gold

    // ─────────────────────────── Events ───────────────────────────────
    private System.Action<int> _onSizeChanged;
    private System.Action<int> _onLuckChanged;
    private System.Action<int> _onMoneyChanged;

    // ──────────────────────────── Unity ───────────────────────────────
    private void Start()
    {
        _onSizeChanged  = _ => RefreshUI();
        _onLuckChanged  = _ => RefreshUI();
        _onMoneyChanged = _ => RefreshUI();

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnSizeLevelChanged += _onSizeChanged;
            UpgradeManager.Instance.OnLuckLevelChanged += _onLuckChanged;
        }

        GameManager.OnMoneyChanged += _onMoneyChanged;

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnSizeLevelChanged -= _onSizeChanged;
            UpgradeManager.Instance.OnLuckLevelChanged -= _onLuckChanged;
        }

        GameManager.OnMoneyChanged -= _onMoneyChanged;
    }

    // ─────────────────────────── Input (world-space raycast) ──────────
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

        if (hit.collider == null) return;

        GameObject hitObj = hit.collider.gameObject;

        if (scratchSizeSlot?.clickTarget != null && hitObj == scratchSizeSlot.clickTarget)
        {
            TryBuyScratchSize();
        }
        else if (scratchLuckSlot?.clickTarget != null && hitObj == scratchLuckSlot.clickTarget)
        {
            TryBuyScratchLuck();
        }
    }

    // ─────────────────────────── Purchase Handlers ────────────────────
    private void TryBuyScratchSize()
    {
        UpgradeManager.Instance?.TryPurchaseScratchSize();
    }

    private void TryBuyScratchLuck()
    {
        UpgradeManager.Instance?.TryPurchaseScratchLuck();
    }

    // ─────────────────────────── Refresh ──────────────────────────────
    public void RefreshUI()
    {
        if (UpgradeManager.Instance == null) return;

        int playerMoney = GameManager.Instance != null ? GameManager.Instance.PlayerMoney : 0;

        RefreshSlot(
            slot:        scratchSizeSlot,
            definition:  UpgradeManager.Instance.ScratchSizeDefinition,
            level:       UpgradeManager.Instance.ScratchSizeLevel,
            cost:        UpgradeManager.Instance.NextSizeCost,
            atMax:       UpgradeManager.Instance.SizeAtMaxLevel,
            playerMoney: playerMoney
        );

        RefreshSlot(
            slot:        scratchLuckSlot,
            definition:  UpgradeManager.Instance.ScratchLuckDefinition,
            level:       UpgradeManager.Instance.ScratchLuckLevel,
            cost:        UpgradeManager.Instance.NextLuckCost,
            atMax:       UpgradeManager.Instance.LuckAtMaxLevel,
            playerMoney: playerMoney
        );
    }

    // ─────────────────────────── Per-slot refresh ─────────────────────
    private void RefreshSlot(UpgradeSlotUI slot, UpgradeDefinition definition,
                             int level, long cost, bool atMax, int playerMoney)
    {
        if (slot == null) return;

        bool canAfford = !atMax && (playerMoney >= cost);

        // ── Name ──────────────────────────────────────────────────────
        if (slot.nameText != null && definition != null)
            slot.nameText.text = definition.upgradeName;

        // ── Icon ──────────────────────────────────────────────────────
        if (slot.iconRenderer != null && definition != null && definition.upgradeIcon != null)
            slot.iconRenderer.sprite = definition.upgradeIcon;

        // ── Level ─────────────────────────────────────────────────────
        if (slot.levelText != null)
            slot.levelText.text = atMax ? $"Lv. {level} (MAX)" : $"Lv. {level}";

        // ── Cost (colour signals affordability) ───────────────────────
        if (slot.costText != null)
        {
            if (atMax)
            {
                slot.costText.text  = "MAXED";
                slot.costText.color = maxLevelColor;
            }
            else
            {
                slot.costText.text  = CurrencyFormatter.FormatMoney(cost);
                slot.costText.color = canAfford ? affordableColor : unaffordableColor;
            }
        }
    }
}