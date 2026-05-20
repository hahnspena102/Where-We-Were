using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;




public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;

    [SerializeField] private GameObject drawingDisplayPrefab;
    [SerializeField]private AudioClip flipPageSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private PromptData[] promptDatas;
    [SerializeField] private CinemachineCamera introCamera;
    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private CinemachineCamera transitionCamera;
    
    [SerializeField, ReadOnly] private PromptData currentPromptData;
    private float elapsedWorldTime;
    
    [SerializeField,ReadOnly] private PromptPanel promptPanel;
    [SerializeField,ReadOnly] private EntryPanel entryPanel;
    [SerializeField,ReadOnly] private DrawPanel drawPanel;
    [SerializeField] private ScriptData introScript;
    [SerializeField] private ScriptData transitionScript;
    private TextDisplay textDisplay;
    
    
    private bool startPrompt;
    private float timeUntilPrompt = 5f;
    private HoverProjector hoverProjector;
    private Vector3 hoverPosition;
    
    private DatabaseManager databaseManager;
    private readonly HashSet<int> displayedEntryIds = new HashSet<int>();

    public Vector3 HoverPosition { get => hoverPosition; set => hoverPosition = value; }
    public GameplayState CurrentState { get => playerData != null ? playerData.CurrentGameplayState : GameplayState.Exploring; set => playerData.CurrentGameplayState = value; }
    public PromptData CurrentPromptData { get => currentPromptData; set => currentPromptData = value; }
    public int CurrentPromptIndex { get => playerData != null ? playerData.PromptIndex : 0; }
    public PromptData[] PromptDatas { get => promptDatas; set => promptDatas = value; }
    public PlayerData PlayerData { get => playerData; set => playerData = value; }

    void Start()
    {
        hoverProjector = FindAnyObjectByType<HoverProjector>();
        databaseManager = FindAnyObjectByType<DatabaseManager>();
        
        currentPromptData = promptDatas[playerData.PromptIndex];


        LoadScene("TextScene");
        LoadScene("UIScene");

        RestartGame();

        
    }

    void OnDestroy()
    {
        if (databaseManager != null)
        {
            databaseManager.RemoteEntryLoaded -= SpawnEntryDisplay;
        }
    }

    void Update()
    {
        if (playerData.CurrentGameState == GameState.Intro)
        {
            IntroUpdate();
        }
        else if (playerData.CurrentGameState == GameState.Gameplay)
        {
            GameplayUpdate();
        }
         else if (playerData.CurrentGameState == GameState.Outro)
        {
            // OutroUpdate();
        }
        

        
    }

    void LoadScene(string sceneName)
    {
        // If the requested scene is already loaded, do nothing.
        Scene target = SceneManager.GetSceneByName(sceneName);
        if (target.isLoaded)
        {
            return;
        }


        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    public void SwitchGameState(GameState newState)
    {
        playerData.CurrentGameState = newState;

        textDisplay.ClearTextMesh();

        if (newState == GameState.Intro)
        {
     
            SoundManager.instance.PlayMusic("space");
            introCamera.Priority = 10;
            gameplayCamera.Priority = 0;

            
            textDisplay.ResetDisplay(introScript, true);

            Debug.Log("Intro script started.");
                
            




            Debug.Log("Switched to Intro Scene");
        } else if (newState == GameState.Transition)
        {
            
            SoundManager.instance.PlayMusic("space");
            introCamera.Priority = 0;
            transitionCamera.Priority = 10;

            textDisplay.ResetDisplay(transitionScript, true);
                  
            

            Debug.Log("Switched to Transition Scene");
        }
        else if (newState == GameState.Gameplay)
        {
            playerData.CurrentGameplayState = GameplayState.Exploring;
            SoundManager.instance.PlayMusic("sunlight");
            SkyboxBlender skyboxBlender = FindAnyObjectByType<SkyboxBlender>();
            if (skyboxBlender != null)            {
                skyboxBlender.StartFade();
            }
            introCamera.Priority = 0;
            gameplayCamera.Priority = 10;

            Debug.Log("Switched to Gameplay Scene");
         

        }
        else if (newState == GameState.Outro)
        {

            gameplayCamera.Priority = 10;
        }
    }

    IEnumerator SwitchingGameState(GameState newState)
    {
        yield return new WaitForSeconds(1.0f); // Optional delay for transition effects
        SwitchGameState(newState);
    }

    

    void LoadData()
    {
        Debug.Log($"Loading {databaseManager.GetAllEntries().Length} entries from database.");
        foreach (Entry entry in databaseManager.GetAllEntries())
        {
            if (entry == null) continue;
            if (entry.promt_id != CurrentPromptIndex)
            {
                Debug.Log($"Skipping load of entry {entry.id} for prompt {entry.promt_id} (current prompt {CurrentPromptIndex})");
                continue;
            }
            SpawnEntryDisplay(entry);
        }
  
    }

    private void SpawnEntryDisplay(Entry entry)
    {
        if (entry == null)
        {
            Debug.LogWarning("SpawnEntryDisplay called with null entry");
            return;
        }

        Debug.Log($"SpawnEntryDisplay called for entry {entry.id} promt_id={entry.promt_id} currentPrompt={CurrentPromptIndex}");

        // Only spawn entries that belong to the current prompt index
        if (entry.promt_id != CurrentPromptIndex)
        {
            Debug.Log($"Skipping entry {entry.id} for prompt {entry.promt_id} (current prompt {CurrentPromptIndex})");
            return;
        }

        if (entry.id > 0 && displayedEntryIds.Contains(entry.id))
        {
            Debug.Log($"Entry {entry.id} already displayed, skipping");
            return;
        }

        GameObject entryGO = Instantiate(drawingDisplayPrefab, entry.position, Quaternion.identity);

        // Apply a large random scale first so bounds calculations include scale
        entryGO.transform.localScale = Vector3.one * UnityEngine.Random.Range(4.0f, 8.0f);

        // Assign sprite if available so renderer bounds will be correct
        SpriteRenderer sr = entryGO.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (entry.sprite != null)
            {
                sr.sprite = entry.sprite;
                Debug.Log($"Set sprite for entry {entry.id}");
            }
            else
            {
                Debug.LogWarning($"Entry {entry.id} has no sprite!");
            }
        }

        // Compute spawn position: on-ground entries should sit on surface (account for prefab height)
        Vector3 entryPosition;
        if (currentPromptData != null && currentPromptData.IsOnGround)
        {
            // Raycast down from a little above the saved position to find the actual ground
            if (Physics.Raycast(entry.position + Vector3.up * 1f, Vector3.down, out RaycastHit groundHit, 10f))
            {
                float yOffset = 0f;
                Renderer ren = sr != null ? (Renderer)sr : entryGO.GetComponent<Renderer>();
                if (ren != null)
                {
                    yOffset = ren.bounds.extents.y;
                }

                entryPosition = groundHit.point + Vector3.up * yOffset;
            }
            else
            {
                entryPosition = entry.position;
            }
        }
        else
        {
            entryPosition = entry.position + new Vector3(0, UnityEngine.Random.Range(5f, 12f), 0);
        }

        entryGO.transform.position = entryPosition;

        DrawingDisplay dd = entryGO.GetComponent<DrawingDisplay>();
        if (dd != null)
        {
            dd.Entry = entry;
        }

        if (entry.id > 0)
        {
            displayedEntryIds.Add(entry.id);
        }

        Debug.Log($"Spawned entry ID {entry.id} at position {entry.position}, sprite is {(sr != null && sr.sprite != null ? "set" : "null")}");
    }

   

    public void IntroUpdate()
    {
       Debug.Log("Updating the intro");
    }

    public void GameplayUpdate()
    {
        if (promptPanel == null)
        {
            promptPanel = FindAnyObjectByType<PromptPanel>();
        }
        if (entryPanel == null)
        {
            entryPanel = FindAnyObjectByType<EntryPanel>();
        }
        if (drawPanel == null)        {
            drawPanel = FindAnyObjectByType<DrawPanel>();
        }

        elapsedWorldTime += Time.deltaTime;
        
        if (elapsedWorldTime < 1.0f) return; 

        if (elapsedWorldTime >= timeUntilPrompt && startPrompt == false) 
        {
            promptPanel.StartPrompt(currentPromptData.PromptText);
            startPrompt = true;
            playerData.CurrentGameplayState = GameplayState.Prompting;
        }

   

        if (playerData.CurrentGameplayState == GameplayState.Prompting)
        {
            hoverProjector.ShowHover();
            hoverPosition = hoverProjector.HoverProject();
            
        }
    }

    public void PlayerHold()
    {
        if (playerData.CurrentGameplayState != GameplayState.Prompting) return;
        Debug.Log("Player is holding the prompt.");
        
        playerData.CurrentGameplayState = GameplayState.Answering;
        promptPanel.HidePrompt();
        entryPanel.StartEntry(currentPromptData.TypeInstruction);
    }

    public void NextPage()
    {
        if (playerData.CurrentGameplayState != GameplayState.Answering && playerData.CurrentGameplayState != GameplayState.Drawing) return;
        
        if (playerData.CurrentGameplayState == GameplayState.Answering)
        {
            entryPanel.HideEntry();
            drawPanel.StartEntry(currentPromptData.DrawInstruction);
            playerData.CurrentGameplayState = GameplayState.Drawing;

            audioSource.PlayOneShot(flipPageSound);
        }
        else if (playerData.CurrentGameplayState == GameplayState.Drawing)
        {
            drawPanel.HideEntry();

            Texture2D processed = drawPanel.GetProcessedTexture();
            hoverProjector.HideHover();

            audioSource.PlayOneShot(flipPageSound);

            playerData.CurrentGameplayState = GameplayState.Explaining;


            Entry entry = databaseManager.AddEntry(hoverPosition, entryPanel.GetEntryText(), processed);
            SpawnEntryDisplay(entry);
            StartCoroutine(PromptEnding());

            
            
        }
    }

    IEnumerator PromptEnding()
    {

        yield return new WaitForSeconds(0.5f); 
        ScriptData afterDrawScript = currentPromptData.AfterDrawScript;
        Debug.Log($"textDisplay is {(textDisplay != null ? "not null" : "null")}, afterDrawScript is {(afterDrawScript != null ? "not null" : "null")}");
        if (textDisplay != null)
            textDisplay.ResetDisplay(afterDrawScript, false);

        
    }

    public void PreviousPage()
    {
        if (playerData.CurrentGameplayState != GameplayState.Answering && playerData.CurrentGameplayState != GameplayState.Drawing) return;
        
        if (playerData.CurrentGameplayState == GameplayState.Answering)
        {
            entryPanel.HideEntry();
            promptPanel.StartPrompt(currentPromptData.PromptText);
            playerData.CurrentGameplayState = GameplayState.Prompting;
            audioSource.PlayOneShot(flipPageSound);

        }
        else if (playerData.CurrentGameplayState == GameplayState.Drawing)
        {
            drawPanel.HideEntry();
            entryPanel.StartEntry(currentPromptData.TypeInstruction);
            playerData.CurrentGameplayState = GameplayState.Answering;
            audioSource.PlayOneShot(flipPageSound);
        }
    }

    public void RestartGame()
    {
        StartCoroutine(RestartGameCoroutine());
    }

    IEnumerator RestartGameCoroutine()
    {
        yield return new WaitForSeconds(0.5f); // Small delay to ensure scenes are loaded
        textDisplay = FindAnyObjectByType<TextDisplay>();
        entryPanel = FindAnyObjectByType<EntryPanel>();
        drawPanel = FindAnyObjectByType<DrawPanel>();

        currentPromptData = promptDatas[playerData.PromptIndex];
        startPrompt = false;
        elapsedWorldTime = 0f;
        playerData.CurrentGameplayState = GameplayState.Exploring;

        
        // Only delete drawings from the current prompt session, not all entries
        // DeleteAllDrawingDisplays();

        if (databaseManager != null)
        {
            databaseManager.RemoteEntryLoaded += SpawnEntryDisplay;
        }

        currentPromptData = promptDatas[playerData.PromptIndex];

        // Remove any existing spawned drawings from the previous prompt
        DeleteAllDrawingDisplays();

        if (databaseManager != null)
        {
            databaseManager.ClearCachedEntriesForCurrentPrompt();
        }

        if (databaseManager != null)
        {
            databaseManager.RefreshEntries();
        }

        LoadData();


        if (entryPanel != null)
        {
            entryPanel.ResetPanel();
        } else
        {
            Debug.LogWarning("EntryPanel not found when trying to reset.");
        }
        if (drawPanel != null)
        {
            drawPanel.ResetPanel();
        } else
        {
            Debug.LogWarning("DrawPanel not found when trying to reset.");
        }


        if (playerData.CurrentGameState == GameState.Intro)
        {
            SwitchGameState(GameState.Intro);
        } else if (playerData.CurrentGameState == GameState.Gameplay)
        {
            SwitchGameState(GameState.Gameplay);
        } else if (playerData.CurrentGameState == GameState.Transition)
        {
            SwitchGameState(GameState.Transition);
        }
        else
        {
           SwitchGameState(GameState.Outro);
        }
    }

    public void DeleteAllDrawingDisplays()
    {
        foreach (DrawingDisplay dd in FindObjectsByType<DrawingDisplay>())
        {
            Destroy(dd.gameObject);
        }
        displayedEntryIds.Clear();
    }

    public void NextPrompt()
    {
        playerData.PromptIndex = (playerData.PromptIndex + 1) % promptDatas.Length;
        currentPromptData = promptDatas[playerData.PromptIndex];
        if (playerData.PromptIndex >= promptDatas.Length)
        {
            return;
        }
        StartCoroutine(TransititionToNextPrompt());
       
    }

    IEnumerator TransititionToNextPrompt()
    {
        transitionCamera.Priority = 11;
        SkyboxBlender skyboxBlender = FindAnyObjectByType<SkyboxBlender>();
        skyboxBlender.StartFade(false);
        yield return new WaitForSeconds(skyboxBlender.BlendDuration + 0.5f);
         yield return new WaitForSeconds(0.5f);
         playerData.CurrentGameState = GameState.Transition;
       
         
        RestartGame();
    }

    

  
}
