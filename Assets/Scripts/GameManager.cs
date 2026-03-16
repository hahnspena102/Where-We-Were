using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Exploring,
    Prompting,
    Answering,
    Drawing,
    Reviewing

}


public class GameManager : MonoBehaviour
{
    private float elapsedWorldTime;
    private PromptPanel promptPanel;
    private EntryPanel entryPanel;
    private DrawPanel drawPanel;
    private DrawingDisplay drawingDisplay;
    private bool startPrompt;
    private float timeUntilPrompt = 5f;
    private HoverProjector hoverProjector;
    private GameState currentState = GameState.Exploring;
    private Vector3 hoverPosition;

    public Vector3 HoverPosition { get => hoverPosition; set => hoverPosition = value; }
    public GameState CurrentState { get => currentState; set => currentState = value; }

    void Start()
    {
        promptPanel = FindFirstObjectByType<PromptPanel>();
        entryPanel = FindFirstObjectByType<EntryPanel>();
        drawPanel = FindFirstObjectByType<DrawPanel>();
        hoverProjector = FindFirstObjectByType<HoverProjector>();
        drawingDisplay = FindFirstObjectByType<DrawingDisplay>();
    }
    void Awake() {
        int countLoaded = SceneManager.sceneCount;
        Scene[] loadedScenes = new Scene[countLoaded];

        for (int i = 0; i < countLoaded; i++)
        {
            loadedScenes[i] = SceneManager.GetSceneAt(i);
            if (loadedScenes[i].name == "UIScene")
            {
                return;
            }
        }
        
        SceneManager.LoadSceneAsync("UIScene", LoadSceneMode.Additive);

    }

    void Update()
    {
        Debug.Log("Current State: " + currentState);

        elapsedWorldTime += Time.deltaTime;

        if (elapsedWorldTime >= timeUntilPrompt && startPrompt == false) 
        {
            promptPanel.StartPrompt("Recall a place you felt alone.");
            startPrompt = true;
            currentState = GameState.Prompting;
        }

   

        if (currentState == GameState.Prompting)
        {
            hoverPosition = hoverProjector.HoverProject();
        }
    }

    public void PlayerHold()
    {
        if (currentState != GameState.Prompting) return;
        Debug.Log("Player is holding the prompt.");
        
        currentState = GameState.Answering;
        promptPanel.HidePrompt();
        entryPanel.StartEntry("Describe the place you recalled.");
    }

    public void NextPage()
    {
        if (currentState != GameState.Answering && currentState != GameState.Drawing) return;
        
        if (currentState == GameState.Answering)
        {
            entryPanel.HideEntry();
            drawPanel.StartEntry("Draw a star!");
            currentState = GameState.Drawing;
        }
        else if (currentState == GameState.Drawing)
        {
            drawPanel.HideEntry();

            Texture2D processed = drawPanel.GetProcessedTexture();
            drawingDisplay.ShowDrawing(processed);
            drawingDisplay.transform.position = hoverPosition + new Vector3(0, 20f, 0); 
            hoverProjector.HideHover();
            currentState = GameState.Reviewing;
        }
    }

    public void PreviousPage()
    {
        if (currentState != GameState.Answering && currentState != GameState.Drawing) return;
        
        if (currentState == GameState.Answering)
        {
            entryPanel.HideEntry();
            promptPanel.StartPrompt("Recall a place you felt alone.");
            currentState = GameState.Prompting;
        }
        else if (currentState == GameState.Drawing)
        {
            drawPanel.HideEntry();
            entryPanel.StartEntry("Describe the place you recalled.");
            currentState = GameState.Answering;
        }
    }
}
