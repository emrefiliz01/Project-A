using UnityEngine;
using System;

public class ScratchCard : MonoBehaviour
{
    [SerializeField] private int baseBrushRadius = 20;

    public int brushRadius
    {
        get
        {
            if (UpgradeManager.Instance != null)
                return UpgradeManager.Instance.CurrentBrushRadius;
            return baseBrushRadius;
        }
        set
        {
            baseBrushRadius = value;
        }
    }
    private SpriteRenderer spriteRenderer;
    private Texture2D scratchTex;

    public bool IsScratchable { get; set; } = false;

    [Range(0.1f, 1.0f)]
    public float scratchThreshold = 0.90f;

    [Header("Localized Reward Sub-Region")]
    [SerializeField] private bool useLocalizedRewardCheck = false;
    [SerializeField] private Rect rewardSymbolBounds = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
    [Range(0.1f, 1.0f)]
    [SerializeField] private float symbolZoneThreshold = 0.85f;

    public bool UseLocalizedRewardCheck
    {
        get => useLocalizedRewardCheck;
        set => useLocalizedRewardCheck = value;
    }

    public Rect RewardSymbolBounds
    {
        get => rewardSymbolBounds;
        set => rewardSymbolBounds = value;
    }

    public float SymbolZoneThreshold
    {
        get => symbolZoneThreshold;
        set => symbolZoneThreshold = value;
    }

    public Action<float> OnScratched;
    private bool[] isClearedArray;
    private int initialSolidPixels = 0;
    private int currentSolidPixels = 0;
    private int initialSymbolSolidPixels = 0;
    private int currentSymbolSolidPixels = 0;
    private bool isInitialized = false;
    private bool isCompleted = false;

    public bool IsCompleted => isCompleted || CheckIsCompleted();

    void Start()
    {
        if (!isInitialized)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            Initialize(spriteRenderer != null ? spriteRenderer.sprite : null);
        }
    }

    public void Initialize(Sprite coverSprite)
    {
        if (isInitialized) return;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (coverSprite != null)
        {
            spriteRenderer.sprite = coverSprite;
        }

        if (spriteRenderer.sprite == null)
        {
            Debug.LogWarning("ScratchCard: No sprite found for scratching cover!");
            return;
        }

        isInitialized = true;

        Texture2D originalTex = spriteRenderer.sprite.texture;

        scratchTex = DuplicateTexture(originalTex);

        spriteRenderer.sprite = Sprite.Create(
            scratchTex,
            spriteRenderer.sprite.rect,
            new Vector2(0.5f, 0.5f),
            spriteRenderer.sprite.pixelsPerUnit
        );

        int w = scratchTex.width;
        int h = scratchTex.height;
        isClearedArray = new bool[w * h];
        initialSolidPixels = 0;
        initialSymbolSolidPixels = 0;

        Color[] pixels = scratchTex.GetPixels();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int index = y * w + x;
                if (pixels[index].a <= 0.1f)
                {
                    isClearedArray[index] = true;
                }
                else
                {
                    initialSolidPixels++;
                    float normX = (float)x / w;
                    float normY = (float)y / h;
                    if (rewardSymbolBounds.Contains(new Vector2(normX, normY)))
                    {
                        initialSymbolSolidPixels++;
                    }
                }
            }
        }
        currentSolidPixels = initialSolidPixels;
        currentSymbolSolidPixels = initialSymbolSolidPixels;
        isCompleted = false;

        // Ensure a Collider2D is present on this GameObject for raycasts and mouse events
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                box.size = spriteRenderer.sprite.rect.size / spriteRenderer.sprite.pixelsPerUnit;
            }
        }
    }

    void Update()
    {
        if (!IsScratchable) return;

        if (Input.GetMouseButton(0))
        {
            if (Camera.main == null || spriteRenderer == null || spriteRenderer.sprite == null || scratchTex == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);

            bool isOverThisCard = false;
            CardZoomController myZoomController = GetComponentInParent<CardZoomController>();

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                if (hit.collider.gameObject == gameObject 
                    || hit.collider.transform.IsChildOf(transform) 
                    || transform.IsChildOf(hit.collider.transform)
                    || (myZoomController != null && hit.collider.GetComponentInParent<CardZoomController>() == myZoomController))
                {
                    isOverThisCard = true;
                    break;
                }
            }

            if (isOverThisCard)
            {
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(Camera.main.transform.position.z - transform.position.z)));
                Vector3 localPos = transform.InverseTransformPoint(mouseWorld);
                Bounds bounds = spriteRenderer.sprite.bounds;

                float px = (localPos.x - bounds.min.x) / bounds.size.x;
                float py = (localPos.y - bounds.min.y) / bounds.size.y;

                if (px >= 0f && px <= 1f && py >= 0f && py <= 1f)
                {
                    int tx = Mathf.FloorToInt(px * scratchTex.width);
                    int ty = Mathf.FloorToInt(py * scratchTex.height);
                    EraseCircle(tx, ty);
                }
            }
        }
    }

    private void OnMouseDown()
    {
        if (!IsScratchable)
        {
            CardZoomController czc = GetComponentInParent<CardZoomController>();
            if (czc != null) czc.HandleChildMouseDown();
        }
    }

    private void OnMouseDrag()
    {
        if (!IsScratchable)
        {
            CardZoomController czc = GetComponentInParent<CardZoomController>();
            if (czc != null) czc.HandleChildMouseDrag();
        }
    }

    private void OnMouseUp()
    {
        if (!IsScratchable)
        {
            CardZoomController czc = GetComponentInParent<CardZoomController>();
            if (czc != null) czc.HandleChildMouseUp();
        }
    }

    void EraseCircle(int cx, int cy)
    {
        bool changed = false;
        int w = scratchTex.width;
        int h = scratchTex.height;

        for (int y = -brushRadius; y <= brushRadius; y++)
        {
            for (int x = -brushRadius; x <= brushRadius; x++)
            {
                if (x * x + y * y <= brushRadius * brushRadius)
                {
                    int px = cx + x;
                    int py = cy + y;
                    
                    if (px >= 0 && px < w && py >= 0 && py < h)
                    {
                        int index = py * w + px;
                        if (!isClearedArray[index])
                        {
                            isClearedArray[index] = true;
                            scratchTex.SetPixel(px, py, Color.clear);
                            currentSolidPixels--;

                            float normX = (float)px / w;
                            float normY = (float)py / h;
                            if (rewardSymbolBounds.Contains(new Vector2(normX, normY)))
                            {
                                currentSymbolSolidPixels--;
                            }

                            changed = true;
                        }
                    }
                }
            }
        }

        if (changed)
        {
            scratchTex.Apply();
            CheckIsCompleted();
            OnScratched?.Invoke(GetScratchedPercentage());
        }
    }

    public float GetScratchedPercentage()
    {
        if (initialSolidPixels == 0) return 1f;
        return 1f - ((float)currentSolidPixels / initialSolidPixels);
    }

    public float GetSymbolScratchedPercentage()
    {
        if (initialSymbolSolidPixels == 0) return 1f;
        return 1f - ((float)currentSymbolSolidPixels / initialSymbolSolidPixels);
    }

    public bool CheckIsCompleted()
    {
        if (isCompleted) return true;

        float overallProgress = GetScratchedPercentage();
        if (overallProgress >= scratchThreshold)
        {
            isCompleted = true;
            return true;
        }

        if (useLocalizedRewardCheck)
        {
            float symbolProgress = GetSymbolScratchedPercentage();
            if (symbolProgress >= symbolZoneThreshold)
            {
                isCompleted = true;
                return true;
            }
        }

        return false;
    }


    public void ClearAll()
    {
        if (scratchTex == null || !isInitialized) return;

        int w = scratchTex.width;
        int h = scratchTex.height;
        Color[] clearPixels = new Color[w * h];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.clear;
            isClearedArray[i] = true;
        }
        scratchTex.SetPixels(clearPixels);
        scratchTex.Apply();
        currentSolidPixels = 0;
        currentSymbolSolidPixels = 0;
        isCompleted = true;
        OnScratched?.Invoke(1.0f);
    }

    private Texture2D DuplicateTexture(Texture2D source)
    {
        if (source == null) return null;

        if (source.isReadable)
        {
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.SetPixels(source.GetPixels());
            copy.Apply();
            return copy;
        }

        RenderTexture renderTex = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);

        Graphics.Blit(source, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;

        Texture2D readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);

        return readableTex;
    }
}
