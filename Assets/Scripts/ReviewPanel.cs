using UnityEngine;
using UnityEngine.UI;

public class ReviewPanel : MonoBehaviour
{
    [SerializeField] private Entry currentEntry;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CanvasGroup entryCanvasGroup;
    [SerializeField] private Image drawingRenderer;
    [SerializeField] private TMPro.TextMeshProUGUI answerText;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Button nextPromptButton;
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
        if (gameManager.CurrentState != GameState.Reviewing)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // if final prompt, hide next button
        if (gameManager.CurrentPromptIndex >= gameManager.PromptDatas.Length - 1)
        {            
            nextPromptButton.gameObject.SetActive(false);
        }

        currentEntry = player.CurrentHoverEntry;
        if (currentEntry != null)
        {
                entryCanvasGroup.alpha = 1f;
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
            entryCanvasGroup.alpha = 0f;
        }
    }

    public void NextPrompt()
    {
        gameManager.NextPrompt();
        Debug.Log("Next Prompt");
    }


}
