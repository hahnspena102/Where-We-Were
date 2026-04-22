using UnityEngine;
using UnityEngine.UI;

public class ReviewPanel : MonoBehaviour
{
    [SerializeField] private Entry currentEntry;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image drawingRenderer;
    [SerializeField] private TMPro.TextMeshProUGUI answerText;
    [SerializeField] private GameManager gameManager;
    private Player player;

    public Entry CurrentEntry { get => currentEntry; set => currentEntry = value; }

    // Start is called before the first frame update
    void Start()
    {
        player = FindFirstObjectByType<Player>();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        currentEntry = player.CurrentHoverEntry;
        if (currentEntry != null && gameManager.CurrentState == GameState.Reviewing)
        {
                canvasGroup.alpha = 1f;
            if (currentEntry.sprite != null)
            {
                drawingRenderer.sprite = currentEntry.sprite;
            }
            else
            {
                drawingRenderer.sprite = null; 
            }

            answerText.text = currentEntry.answer;
        }
        else
        {
            drawingRenderer.sprite = null;
            answerText.text = string.Empty;
            canvasGroup.alpha = 0f;
        }
    }


}
