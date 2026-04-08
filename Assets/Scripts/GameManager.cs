using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.IO;

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
    [SerializeField] private GameObject drawingDisplayPrefab;
    private float elapsedWorldTime;
    private PromptPanel promptPanel;
    private EntryPanel entryPanel;
    private DrawPanel drawPanel;
    private bool startPrompt;
    private float timeUntilPrompt = 5f;
    private HoverProjector hoverProjector;
    private GameState currentState = GameState.Exploring;
    private Vector3 hoverPosition;
    
    private DatabaseManager databaseManager;

    public Vector3 HoverPosition { get => hoverPosition; set => hoverPosition = value; }
    public GameState CurrentState { get => currentState; set => currentState = value; }

    void Start()
    {
        promptPanel = FindFirstObjectByType<PromptPanel>();
        entryPanel = FindFirstObjectByType<EntryPanel>();
        drawPanel = FindFirstObjectByType<DrawPanel>();
        hoverProjector = FindFirstObjectByType<HoverProjector>();
        databaseManager = FindFirstObjectByType<DatabaseManager>();

        LoadData();
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

    void LoadData()
    {
        Debug.Log($"Loading {databaseManager.GetAllEntries().Length} entries from database.");
        foreach (Entry entry in databaseManager.GetAllEntries())
        {
            GameObject entryGO = Instantiate(drawingDisplayPrefab, entry.position, Quaternion.identity);
            entryGO.transform.localPosition = entry.position + new Vector3(0, UnityEngine.Random.Range(5f, 12f), 0);
            entryGO.transform.localScale = Vector3.one * UnityEngine.Random.Range(2.0f, 8.0f);
            SpriteRenderer sr = entryGO.GetComponent<SpriteRenderer>();
            if (sr != null)            {
                sr.sprite = entry.sprite;
            }
            DrawingDisplay dd = entryGO.GetComponent<DrawingDisplay>();
            if (dd != null)            {
                dd.ShowDrawing(entry.sprite.texture);
            }
            Debug.Log($"Loaded entry ID {entry.id} at position {entry.position}");
            
            
        }
  
    }

    void Update()
    {
        //Debug.Log("Current State: " + currentState);

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

            GameObject drawingGO = Instantiate(drawingDisplayPrefab, hoverPosition + new Vector3(0, 8f, 0), Quaternion.identity);
            SpriteRenderer sr = drawingGO.GetComponent<SpriteRenderer>();
            if (sr != null)            {
                Sprite drawingSprite = Sprite.Create(processed, new Rect(0, 0, processed.width, processed.height), new Vector2(0.5f, 0.5f));
                sr.sprite = drawingSprite;
            }
            drawingGO.transform.localScale = Vector3.one * 5f;
            DrawingDisplay dd = drawingGO.GetComponent<DrawingDisplay>();
            if (dd != null)            {
                dd.ShowDrawing(processed);
            }
            hoverProjector.HideHover();

            currentState = GameState.Reviewing;

            Entry entry = databaseManager.AddEntry(hoverPosition, entryPanel.GetEntryText(), processed);
            

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
