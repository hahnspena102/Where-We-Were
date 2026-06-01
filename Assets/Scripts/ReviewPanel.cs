using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReviewPanel : MonoBehaviour
{
    [SerializeField] private Entry currentEntry;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CanvasGroup entryCanvasGroup;
    [SerializeField] private Image drawingRenderer;
    [SerializeField] private TMPro.TextMeshProUGUI answerText;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Button nextPromptButton;
    [SerializeField] private Button[] promptButtons;
    private Player player;

    public Entry CurrentEntry { get => currentEntry; set => currentEntry = value; }

    // Start is called before the first frame update
    void Start()
    {
        player = FindAnyObjectByType<Player>();
        gameManager = FindAnyObjectByType<GameManager>();

        if (drawingRenderer != null)
        {
            drawingRenderer.raycastTarget = false;
        }

        if (answerText != null)
        {
            answerText.raycastTarget = false;
        }

        if (entryCanvasGroup != null)
        {
            entryCanvasGroup.blocksRaycasts = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager.CurrentState != GameplayState.Reviewing)
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

        if (player.PlayerData.CurrentGameState == GameState.Outro)
        {
            // hide next button during outro
            nextPromptButton.gameObject.SetActive(false);
            //show all prompt buttons during outro
            foreach (Button button in promptButtons)            {
                button.gameObject.SetActive(true);
            }

        } else
        {
            foreach (Button button in promptButtons)            {
                button.gameObject.SetActive(false);
            }
             nextPromptButton.gameObject.SetActive(true);
        }

        // if final prompt, hide next button
        if (gameManager.CurrentPromptIndex >= gameManager.PromptDatas.Length - 1)
        {            
            TextMeshProUGUI buttonText = nextPromptButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)            {
                buttonText.text = "End Game";
            }
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

            player.PlayerData.AddEntryRead(currentEntry);

            answerText.text = currentEntry.answer;
        }
        else
        {
            drawingRenderer.sprite = null;
            answerText.text = string.Empty;
            entryCanvasGroup.alpha = 0f;
        }

        int numEntriesRead = player.PlayerData.GetNumEntriesReadForPrompt(gameManager.CurrentPromptIndex);
        
        if (numEntriesRead >= 2)
        {
            nextPromptButton.interactable = true;
        }
        else
        {
            nextPromptButton.interactable = false;
        }
    }

    public void NextPrompt()
    {
        //hide panel
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Ensure the gameplay state is not `Reviewing` so the panel stays hidden immediately
        if (gameManager != null && gameManager.PlayerData != null)
        {
            gameManager.PlayerData.CurrentGameplayState = GameplayState.Exploring;
        }

        if (gameManager.CurrentPromptIndex >= gameManager.PromptDatas.Length - 1)
        {
            gameManager.EndGame();
            Debug.Log("End Game");
            return;
        } else
        {
            gameManager.NextPrompt();
            Debug.Log("Next Prompt");
        }
     
    }

    public void ReviewPrompt(int promptIndex)
    {
        gameManager.ReviewPrompt(promptIndex);
    }


}
