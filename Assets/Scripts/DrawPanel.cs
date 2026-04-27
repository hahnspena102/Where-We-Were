using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class DrawPanel : MonoBehaviour
{
    private RectTransform panelRect;
    private CanvasGroup canvasGroup;

    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Drawing")]
    [SerializeField] private RawImage drawCanvas;
    [SerializeField] private Color currentColor = Color.black;
    private Texture2D drawTexture;
    private Color[] clearColors;

    private int resolution = 64;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference mousePositionAction;
    private int brushSize = 1;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PromptData promptData;
    
    private Vector2 previousMousePos = Vector2.zero;
    private bool wasMousePressed = false;

    public Color CurrentColor { get => currentColor; set => currentColor = value; }

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelRect = GetComponent<RectTransform>();

        gameManager = FindFirstObjectByType<GameManager>();
        InitCanvas();

        currentColor = promptData.ColorPalette[0];
        //StartEntry("Describe the place you recalled.");
    }

    void Update()
    {
        if (canvasGroup.interactable) HandleDrawing();
    }

    void InitCanvas()
    {
        drawTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        drawTexture.filterMode = FilterMode.Point;

        clearColors = new Color[resolution * resolution];

        for (int i = 0; i < clearColors.Length; i++)
            clearColors[i] = Color.white;

        drawTexture.SetPixels(clearColors);
        drawTexture.Apply();

        drawCanvas.texture = drawTexture;
    }

    void HandleDrawing()
    {
        bool isMousePressed = clickAction.action.IsPressed();
        
        if (!isMousePressed)
        {
            wasMousePressed = false;
            return;
        }

        RectTransform rect = drawCanvas.rectTransform;

        Vector2 localMousePos;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect,
            mousePositionAction.action.ReadValue<Vector2>(),
            null,
            out localMousePos))
            return;

        Rect r = rect.rect;

        float x = (localMousePos.x - r.x) / r.width;
        float y = (localMousePos.y - r.y) / r.height;

        int px = Mathf.FloorToInt(x * resolution);
        int py = Mathf.FloorToInt(y * resolution);

        if (px >= 0 && px < resolution && py >= 0 && py < resolution)
        {
            bool pixelChanged = false;

            // If this is the first frame of drawing, just draw at current position
            if (!wasMousePressed)
            {
                DrawBrush(px, py, ref pixelChanged);
                wasMousePressed = true;
            }
            else
            {
                // Draw a line from previous position to current position
                DrawLine(previousMousePos, new Vector2(px, py), ref pixelChanged);
            }

            previousMousePos = new Vector2(px, py);

            if (pixelChanged)
                drawTexture.Apply();
        }
    }

    void DrawBrush(int centerX, int centerY, ref bool pixelChanged)
    {
        for (int xOffset = -brushSize; xOffset <= brushSize; xOffset++)
        {
            for (int yOffset = -brushSize; yOffset <= brushSize; yOffset++)
            {
                int drawX = centerX + xOffset;
                int drawY = centerY + yOffset;

                if (drawX >= 0 && drawX < resolution && drawY >= 0 && drawY < resolution)
                {
                    drawTexture.SetPixel(drawX, drawY, currentColor);
                    pixelChanged = true;
                }
            }
        }
    }

    void DrawLine(Vector2 from, Vector2 to, ref bool pixelChanged)
    {
        float distance = Vector2.Distance(from, to);
        int steps = Mathf.CeilToInt(distance);

        for (int i = 0; i <= steps; i++)
        {
            float t = steps > 0 ? i / (float)steps : 0;
            Vector2 pos = Vector2.Lerp(from, to, t);
            DrawBrush(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), ref pixelChanged);
        }
    }

    public void StartEntry(string question)
    {
        StartCoroutine(PromptEntry(question));
    }

    public void HideEntry()
    {
        StartCoroutine(HideEntryCoroutine());
    }

    IEnumerator HideEntryCoroutine()
    {
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime / fadeDuration;
            yield return null;
        }
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    IEnumerator PromptEntry(string question)
    {
        promptText.text = question;

        while (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime / fadeDuration;
            yield return null;
        }
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

        public void NextPage()
    {
        gameManager.NextPage();
    }

    public void PreviousPage()
    {
        gameManager.PreviousPage();
    }

    public Texture2D GetProcessedTexture()
    {
        Texture2D processed = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        processed.filterMode = FilterMode.Point;

        Color[] pixels = drawTexture.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            float brightness = (pixels[i].r + pixels[i].g + pixels[i].b) / 3f;

            // treat very bright pixels as background
            if (brightness > 0.9f)
            {
                pixels[i] = new Color(0, 0, 0, 0);
            }
            else
            {
                pixels[i] = new Color(pixels[i].r, pixels[i].g, pixels[i].b, 1);
            }
        }

        processed.SetPixels(pixels);
        processed.Apply();

        return processed;
    }

    public void ClearCanvas()
    {
        drawTexture.SetPixels(clearColors);
        drawTexture.Apply();
    }
}