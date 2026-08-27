using UnityEngine;
using TMPro;

/// <summary>
/// Drives the two upgrade button containers in the Upgrade Panel UI.
///
/// ─── WORLD-SPACE VERSION ─────────────────────────────────────────────────
/// Works exactly like GameManager's buy buttons: no Canvas, no UI Button
/// component needed. Click detection uses Physics2D.GetRayIntersection, so
/// each upgrade container just needs a Collider2D (e.g. BoxCollider2D).
///
/// HOW TO SET UP IN THE SCENE
/// ──────────────────────────
/// 1. Attach this script to any persistent GameObject (e.g. GameManager or
///    a dedicated "UpgradeUI" empty object).
/// 2. Expand the two slots in the Inspector and fill in:
///      • Click Target  → the world-space GameObject the player clicks
///                        (BuyScratchSize / BuyScratchLuck).
///                        ⚠ It MUST have a Collider2D for raycasting to work.
///      • Name Text     → TextMeshPro (world-space or Canvas) showing the name
///      • Cost Text     → TextMeshPro showing the price
///      • Level Text    → TextMeshPro showing the current level
///      • Icon Renderer → SpriteRenderer showing the upgrade icon
///                        (leave empty if you don't have an icon sprite)
/// 3. Make sure UpgradeManager exists in the scene with its UpgradeDefinition
///    assets assigned.
/// ─────────────────────────────────────────────────────────────────────────
/// </summary>
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
        // Cache delegates so we can unsubscribe the same instance in OnDestroy.
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
        // RefreshUI() fires automatically via OnSizeLevelChanged / OnMoneyChanged.
    }

    private void TryBuyScratchLuck()
    {
        UpgradeManager.Instance?.TryPurchaseScratchLuck();
        // RefreshUI() fires automatically via OnLuckLevelChanged / OnMoneyChanged.
    }

    // ─────────────────────────── Refresh ──────────────────────────────
    /// <summary>
    /// Refreshes both upgrade slots to reflect the current UpgradeManager state.
    /// Called automatically on every money / level change. Safe to call manually.
    /// </summary>
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
                             int level, int cost, bool atMax, int playerMoney)
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
                slot.costText.text  = $"${cost}";
                slot.costText.color = canAfford ? affordableColor : unaffordableColor;
            }
        }
    }
}
