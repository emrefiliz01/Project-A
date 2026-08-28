using UnityEngine;

/// <summary>
/// Renders a visual software cursor in camera view following the mouse position.
///
/// WHY THIS IS NEEDED:
/// Hardware cursors (Cursor.SetCursor) are drawn by Windows outside of Unity's
/// camera frame buffer, so Unity Recorder does NOT capture them.
///
/// This script renders your cursor directly into the camera frame with a high sorting order
/// and scales it to match the EXACT screen pixel size of standard desktop cursors.
/// </summary>
public class VisualCursorFollower : MonoBehaviour
{
    public static VisualCursorFollower Instance { get; private set; }

    [Header("Cursor Textures (Drag Texture2D / Cursor types here)")]
    [SerializeField] private Texture2D normalCursorTexture;
    [SerializeField] private Texture2D scratchCursorTexture;

    [Header("Cursor Sprites (Alternative - Drag Sprites here)")]
    [SerializeField] private Sprite normalCursorSprite;
    [SerializeField] private Sprite scratchCursorSprite;

    [Header("Size & Scale Settings")]
    [Tooltip("Target size in screen pixels (32 matches standard Windows cursor size, 48 for larger).")]
    [SerializeField] private int targetPixelSize = 32;

    [Tooltip("Fine-tune the size with this slider.")]
    [Range(0.1f, 3.0f)]
    [SerializeField] private float sizeMultiplier = 1.0f;

    [Header("Rendering Settings")]
    [Tooltip("Ensures the visual cursor is rendered on top of cards, table, and UI.")]
    [SerializeField] private int sortingOrder = 1000;
    [SerializeField] private float zDepth = -5f;

    [Tooltip("Hides the default OS cursor while playing.")]
    [SerializeField] private bool hideHardwareCursor = true;

    [Header("Hotspot Offsets (Fine Adjustment)")]
    [SerializeField] private Vector2 normalOffset = Vector2.zero;
    [SerializeField] private Vector2 scratchOffset = Vector2.zero;

    private SpriteRenderer spriteRenderer;
    private Sprite resolvedNormalSprite;
    private Sprite resolvedScratchSprite;
    private bool isScratching = false;

    private void Awake()
    {
        Instance = this;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sortingOrder = sortingOrder;

        // Normal pointer: pivot at top-left (0, 1) so the tip points at the mouse
        resolvedNormalSprite = GetOrCreateSprite(normalCursorSprite, normalCursorTexture, new Vector2(0f, 1f));

        // Scratcher coin/circle: pivot at center (0.5, 0.5) so center scratches
        resolvedScratchSprite = GetOrCreateSprite(scratchCursorSprite, scratchCursorTexture, new Vector2(0.5f, 0.5f));

        if (hideHardwareCursor)
        {
            Cursor.visible = false;
        }

        SetNormal();
    }

    private void Update()
    {
        if (Camera.main == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreen);

        // Position follow
        Vector2 offset = isScratching ? scratchOffset : normalOffset;
        transform.position = new Vector3(worldPos.x + offset.x, worldPos.y + offset.y, zDepth);

        // Dynamic scale calculation to match exact screen pixel size regardless of image resolution
        UpdateCursorScale();
    }

    private void UpdateCursorScale()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || Camera.main == null) return;

        if (Camera.main.orthographic)
        {
            // Height of 1 screen pixel in world units
            float worldPerPixel = (2f * Camera.main.orthographicSize) / Screen.height;
            float desiredWorldHeight = targetPixelSize * sizeMultiplier * worldPerPixel;
            float nativeSpriteHeight = spriteRenderer.sprite.bounds.size.y;

            if (nativeSpriteHeight > 0.0001f)
            {
                float scale = desiredWorldHeight / nativeSpriteHeight;
                transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
        else
        {
            transform.localScale = Vector3.one * (0.25f * sizeMultiplier);
        }
    }

    public void SetNormal()
    {
        isScratching = false;
        if (spriteRenderer != null && resolvedNormalSprite != null)
        {
            spriteRenderer.sprite = resolvedNormalSprite;
        }
    }

    public void SetScratch()
    {
        isScratching = true;
        if (spriteRenderer != null && resolvedScratchSprite != null)
        {
            spriteRenderer.sprite = resolvedScratchSprite;
        }
    }

    private Sprite GetOrCreateSprite(Sprite existingSprite, Texture2D texture, Vector2 pivot)
    {
        if (existingSprite != null) return existingSprite;

        if (texture != null)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                pivot,
                100f
            );
        }
        return null;
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }
}
