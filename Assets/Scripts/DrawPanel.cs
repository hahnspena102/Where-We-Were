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
    private Texture2D drawTexture;
    private Color[] clearColors;

    private int resolution = 256;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference mousePositionAction;
    [SerializeField] private int brushSize = 4;
    [SerializeField] private GameManager gameManager;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelRect = GetComponent<RectTransform>();

        gameManager = FindFirstObjectByType<GameManager>();
        InitCanvas();
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
        if (!clickAction.action.IsPressed())
            return;

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

            for (int xOffset = -brushSize; xOffset <= brushSize; xOffset++)
            {
                for (int yOffset = -brushSize; yOffset <= brushSize; yOffset++)
                {
                    int drawX = px + xOffset;
                    int drawY = py + yOffset;

                    if (drawX >= 0 && drawX < resolution && drawY >= 0 && drawY < resolution)
                    {
                        drawTexture.SetPixel(drawX, drawY, Color.black);
                        pixelChanged = true;
                    }
                }
            }

            if (pixelChanged)
                drawTexture.Apply();
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
                pixels[i] = Color.black;
            }
        }

        processed.SetPixels(pixels);
        processed.Apply();

        return processed;
    }
}