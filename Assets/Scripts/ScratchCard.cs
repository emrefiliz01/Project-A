using UnityEngine;
using System;

public class ScratchCard : MonoBehaviour
{
    public int brushRadius = 20;
    private SpriteRenderer spriteRenderer;
    private Texture2D scratchTex;

    public bool IsScratchable { get; set; } = false;

    [Range(0.1f, 1.0f)]
    public float scratchThreshold = 0.90f;

    public Action<float> OnScratched;
    private bool[] isClearedArray;
    private int initialSolidPixels = 0;
    private int currentSolidPixels = 0;
    private bool isInitialized = false;

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

        Color[] pixels = scratchTex.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a <= 0.1f)
            {
                isClearedArray[i] = true;
            }
            else
            {
                initialSolidPixels++;
            }
        }
        currentSolidPixels = initialSolidPixels;
    }

    void Update()
    {
        if (!IsScratchable)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    CardZoomController zoomController = GetComponentInParent<CardZoomController>();
                    if (zoomController != null)
                    {
                        zoomController.ZoomToScratchMode();
                    }
                }
            }
            return;
        }

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                Vector3 localPos = transform.InverseTransformPoint(hit.point);
                Bounds bounds = spriteRenderer.sprite.bounds;

                float px = (localPos.x - bounds.min.x) / bounds.size.x;
                float py = (localPos.y - bounds.min.y) / bounds.size.y;

                int tx = Mathf.FloorToInt(px * scratchTex.width);
                int ty = Mathf.FloorToInt(py * scratchTex.height);

                EraseCircle(tx, ty);
            }
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
                            changed = true;
                        }
                    }
                }
            }
        }

        if (changed)
        {
            scratchTex.Apply();
            OnScratched?.Invoke(GetScratchedPercentage());
        }
    }

    public float GetScratchedPercentage()
    {
        if (initialSolidPixels == 0) return 1f;
        return 1f - ((float)currentSolidPixels / initialSolidPixels);
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
