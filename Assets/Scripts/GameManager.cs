using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections.Generic;

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
    [SerializeField]private AudioClip flipPageSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private PromptData[] promptDatas;
    [SerializeField] private PromptData currentPromptData;
    [SerializeField] private PlayerData playerData;
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
    private readonly HashSet<int> displayedEntryIds = new HashSet<int>();

    public Vector3 HoverPosition { get => hoverPosition; set => hoverPosition = value; }
    public GameState CurrentState { get => currentState; set => currentState = value; }
    public PromptData CurrentPromptData { get => currentPromptData; set => currentPromptData = value; }
    public int CurrentPromptIndex { get => playerData != null ? playerData.PromptIndex : 0; }
    public PromptData[] PromptDatas { get => promptDatas; set => promptDatas = value; }

    void Start()
    {
        promptPanel = FindFirstObjectByType<PromptPanel>();
        entryPanel = FindFirstObjectByType<EntryPanel>();
        drawPanel = FindFirstObjectByType<DrawPanel>();
        hoverProjector = FindFirstObjectByType<HoverProjector>();
        databaseManager = FindFirstObjectByType<DatabaseManager>();

        if (databaseManager != null)
        {
            databaseManager.RemoteEntryLoaded += SpawnEntryDisplay;
        }

        currentPromptData = promptDatas[playerData.PromptIndex];
        LoadData();
    }

    void OnDestroy()
    {
        if (databaseManager != null)
        {
            databaseManager.RemoteEntryLoaded -= SpawnEntryDisplay;
        }
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
            SpawnEntryDisplay(entry);
        }
  
    }

    private void SpawnEntryDisplay(Entry entry)
    {
        if (entry == null)
        {
            return;
        }

        if (entry.id > 0 && displayedEntryIds.Contains(entry.id))
        {
            return;
        }

        GameObject entryGO = Instantiate(drawingDisplayPrefab, entry.position, Quaternion.identity);
        entryGO.transform.localPosition = entry.position + new Vector3(0, UnityEngine.Random.Range(5f, 12f), 0);
        entryGO.transform.localScale = Vector3.one * UnityEngine.Random.Range(2.0f, 8.0f);

        SpriteRenderer sr = entryGO.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = entry.sprite;
        }

        DrawingDisplay dd = entryGO.GetComponent<DrawingDisplay>();
        if (dd != null)
        {
            dd.Entry = entry;
        }

        if (entry.id > 0)
        {
            displayedEntryIds.Add(entry.id);
        }

        Debug.Log($"Loaded entry ID {entry.id} at position {entry.position}");
    }

    void Update()
    {
        //Debug.Log("Current State: " + currentState);
        

        elapsedWorldTime += Time.deltaTime;
        if (elapsedWorldTime < 0.01f) return; // skip first few frames to allow for initialization

        if (elapsedWorldTime >= timeUntilPrompt && startPrompt == false) 
        {
            promptPanel.StartPrompt(currentPromptData.PromptText);
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
        entryPanel.StartEntry(currentPromptData.PromptText);
    }

    public void NextPage()
    {
        if (currentState != GameState.Answering && currentState != GameState.Drawing) return;
        
        if (currentState == GameState.Answering)
        {
            entryPanel.HideEntry();
            drawPanel.StartEntry(currentPromptData.DrawInstruction);
            currentState = GameState.Drawing;

            audioSource.PlayOneShot(flipPageSound);
        }
        else if (currentState == GameState.Drawing)
        {
            drawPanel.HideEntry();

            Texture2D processed = drawPanel.GetProcessedTexture();
            hoverProjector.HideHover();

            audioSource.PlayOneShot(flipPageSound);

            currentState = GameState.Reviewing;


            Entry entry = databaseManager.AddEntry(hoverPosition, entryPanel.GetEntryText(), processed);
            SpawnEntryDisplay(entry);

        }
    }

    public void PreviousPage()
    {
        if (currentState != GameState.Answering && currentState != GameState.Drawing) return;
        
        if (currentState == GameState.Answering)
        {
            entryPanel.HideEntry();
            promptPanel.StartPrompt(currentPromptData.PromptText);
            currentState = GameState.Prompting;
            audioSource.PlayOneShot(flipPageSound);

        }
        else if (currentState == GameState.Drawing)
        {
            drawPanel.HideEntry();
            entryPanel.StartEntry(currentPromptData.PromptText);
            currentState = GameState.Answering;
            audioSource.PlayOneShot(flipPageSound);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextPrompt()
    {
        playerData.PromptIndex = (playerData.PromptIndex + 1) % promptDatas.Length;
        currentPromptData = promptDatas[playerData.PromptIndex];
        if (playerData.PromptIndex >= promptDatas.Length)
        {
            return;
        }
        RestartGame();
    }
}
