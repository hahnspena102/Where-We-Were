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
    [SerializeField] private ScriptData outroScript;
    [SerializeField] private CinemachineCamera dollyCamera;
    [SerializeField, ReadOnly] private PromptData currentPromptData;
    private float elapsedWorldTime;
    
    [SerializeField,ReadOnly] private PromptPanel promptPanel;
    [SerializeField,ReadOnly] private EntryPanel entryPanel;
    [SerializeField,ReadOnly] private DrawPanel drawPanel;
    [SerializeField] private ScriptData introScript;
    [SerializeField] private ScriptData transitionScript;
    [SerializeField] private GameObject buildingNameCanvasPrefab;
    private TextDisplay textDisplay;
    
    
    private bool startPrompt;
    private float timeUntilPrompt = 5f;
    private HoverProjector hoverProjector;
    private Vector3 hoverPosition;
    
    private DatabaseManager databaseManager;
    private readonly HashSet<int> displayedEntryIds = new HashSet<int>();
    private int previousPromptIndex = -1;

    public Vector3 HoverPosition { get => hoverPosition; set => hoverPosition = value; }
    public GameplayState CurrentState { get => playerData != null ? playerData.CurrentGameplayState : GameplayState.Exploring; set => playerData.CurrentGameplayState = value; }
    public PromptData CurrentPromptData { get => currentPromptData; set => currentPromptData = value; }
    public int CurrentPromptIndex { get => playerData != null ? playerData.PromptIndex : 0; }
    public PromptData[] PromptDatas { get => promptDatas; set => promptDatas = value; }
    public PlayerData PlayerData { get => playerData; set => playerData = value; }
    public GameObject BuildingNameCanvasPrefab { get => buildingNameCanvasPrefab; set => buildingNameCanvasPrefab = value; }

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
            databaseManager.RemoteEntryLoaded -= OnRemoteEntryLoaded;
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
            SoundManager.instance.PlayMusic("space");
            introCamera.Priority = 10;
            gameplayCamera.Priority = 0;

             SkyboxBlender skyboxBlender = FindAnyObjectByType<SkyboxBlender>();
            if (skyboxBlender != null)            {
                skyboxBlender.StartFade(false);
            }

            textDisplay.ResetDisplay(outroScript, true);
        }
    }

    IEnumerator SwitchingGameState(GameState newState)
    {
        yield return new WaitForSeconds(1.0f); // Optional delay for transition effects
        SwitchGameState(newState);
    }

    

    void LoadData()
    {
        if (databaseManager == null)
        {
            Debug.LogWarning("LoadData: databaseManager is null — no entries will be loaded.");
            return;
        }

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

    private void SpawnEntryDisplay(Entry entry, bool ignorePromptFilter = false)
    {
        if (entry == null)
        {
            Debug.LogWarning("SpawnEntryDisplay called with null entry");
            return;
        }

        Debug.Log($"SpawnEntryDisplay called for entry {entry.id} promt_id={entry.promt_id} currentPrompt={CurrentPromptIndex}");

        // Only spawn entries that belong to the current prompt index unless caller requests otherwise
        if (!ignorePromptFilter && entry.promt_id != CurrentPromptIndex)
        {
            Debug.Log($"Skipping entry {entry.id} for prompt {entry.promt_id} (current prompt {CurrentPromptIndex})");
            return;
        }

        // If an instance for this entry ID already exists in the scene (active or inactive), reuse it.
        if (entry.id > 0)
        {
            DrawingDisplay[] allDisplays = FindObjectsOfType<DrawingDisplay>(true);
            foreach (DrawingDisplay existing in allDisplays)
            {
                if (existing == null || existing.Entry == null) continue;
                // Only reuse an existing display if it belongs to the same prompt
                if (existing.Entry.id == entry.id && existing.Entry.promt_id == entry.promt_id)
                {
                    // Re-use the existing display: update entry reference/sprite and keep its world position.
                    existing.Entry = entry;
                    SpriteRenderer existingSr = existing.GetComponent<SpriteRenderer>();
                    if (existingSr != null)
                    {
                        existingSr.sprite = entry.sprite;
                    }
                    existing.gameObject.SetActive(true);

                    if (entry.id > 0)
                    {
                        displayedEntryIds.Add(entry.id);
                    }

                    Debug.Log($"Reused existing display for entry {entry.id}");
                    return;
                }
            }
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

                // Add a small buffer (0.5 units) to prevent clipping into slopes
                entryPosition = groundHit.point + Vector3.up * (yOffset + 0.5f);
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

    // Spawn or re-enable displays for all entries across prompts
    public void ShowAllEntries()
    {
        // Re-enable any existing disabled drawing displays first
        DrawingDisplay[] existing = FindObjectsOfType<DrawingDisplay>(true);
        foreach (DrawingDisplay dd in existing)
        {
            if (dd == null) continue;
            dd.gameObject.SetActive(true);
        }

        if (databaseManager == null)
        {
            databaseManager = FindAnyObjectByType<DatabaseManager>();
        }

        if (databaseManager == null) return;

        // Ensure we have the latest entries from remote
        databaseManager.RefreshEntries();

        // Use the raw cached dataset so all prompts can be shown at once.
        Entry[] entries = databaseManager.GetAllEntriesRaw();

        if (entries == null) return;

        Debug.Log($"ShowAllEntries: preparing to spawn {entries.Length} entries (currentPrompt={CurrentPromptIndex})");

        // Spawn any entries not yet displayed, ignoring prompt filtering
        foreach (Entry entry in entries)
        {
            if (entry == null) continue;
            SpawnEntryDisplay(entry, true);
        }
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
        else if (playerData.CurrentGameplayState == GameplayState.Drawing)
        {
            hoverProjector.ShowHover();
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

        // Ensure database manager reference is current (scenes may have been reloaded)
        if (databaseManager == null)
        {
            databaseManager = FindAnyObjectByType<DatabaseManager>();
        }

        currentPromptData = promptDatas[playerData.PromptIndex];
        startPrompt = false;
        elapsedWorldTime = 0f;
        playerData.CurrentGameplayState = GameplayState.Exploring;

        if (databaseManager != null)
        {
            databaseManager.RemoteEntryLoaded += OnRemoteEntryLoaded;
        }

        currentPromptData = promptDatas[playerData.PromptIndex];

        // Remove any existing spawned drawings from the previous prompt (only)
        if (previousPromptIndex >= 0)
        {
            DeleteDrawingDisplaysForPrompt(previousPromptIndex);
            previousPromptIndex = -1;
        }
        else
        {
            // If no previous prompt recorded, avoid deleting everything — keep existing displays
        }

        // Do not clear cached entries here; keep local dataset intact.
        // databaseManager.ClearCachedEntriesForCurrentPrompt();

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

    // Unload (hide) displays for a specific prompt without clearing cached data.
    public void DeleteDrawingDisplaysForPrompt(int promptIndex)
    {
        // Include inactive objects so we can reliably unload any displays for the prompt.
        DrawingDisplay[] all = FindObjectsOfType<DrawingDisplay>(true);
        foreach (DrawingDisplay dd in all)
        {
            if (dd == null) continue;
            Entry e = dd.Entry;
            if (e != null && e.promt_id == promptIndex)
            {
                // Keep the cached entry IDs so we don't duplicate on reload.
                // Just disable the GameObject to hide it from the scene.
                dd.gameObject.SetActive(false);
            }
        }
    }

    // Re-enable (show) displays for a specific prompt if they were previously unloaded.
    public void ShowDrawingDisplaysForPrompt(int promptIndex)
    {
        // Include inactive objects to find previously-disabled displays
        DrawingDisplay[] all = FindObjectsOfType<DrawingDisplay>(true);
        foreach (DrawingDisplay dd in all)
        {
            if (dd == null) continue;
            Entry e = dd.Entry;
            if (e != null && e.promt_id == promptIndex)
            {
                dd.gameObject.SetActive(true);
            }
        }
    }

    public void NextPrompt()
    {
        int oldIndex = playerData.PromptIndex;
        playerData.PromptIndex = (playerData.PromptIndex + 1) % promptDatas.Length;
        previousPromptIndex = oldIndex;
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

    public void EndGame()
    {
       playerData.CurrentGameState = GameState.Outro;
       SwitchGameState(GameState.Outro);


    }

    public void ReviewPrompt(int promptIndex)
    {
        if (promptIndex < 0 || promptIndex >= promptDatas.Length)
        {
            Debug.LogWarning($"Invalid prompt index {promptIndex} for review");
            return;
        }
        // record the old prompt so we can remove only its displays
        previousPromptIndex = playerData.PromptIndex;

        // switch to the new prompt
        playerData.PromptIndex = promptIndex;
        currentPromptData = promptDatas[promptIndex];

        // Ensure we have a database manager reference
        if (databaseManager == null)
        {
            databaseManager = FindAnyObjectByType<DatabaseManager>();
        }

        // Remove displays only for the previous prompt
        if (previousPromptIndex >= 0)
        {
            DeleteDrawingDisplaysForPrompt(previousPromptIndex);
        }

        // Refresh database entries for the newly selected prompt.
        // Do not clear cached entries here; keep local dataset intact.
        // databaseManager.ClearCachedEntriesForCurrentPrompt();

        // Start async reload: allow linked database to fetch remote entries before loading.
        StartCoroutine(ReloadEntriesCoroutine());

        // Enter reviewing gameplay state and set cameras/UI as appropriate
        playerData.CurrentGameplayState = GameplayState.Reviewing;
        ToReviewOutro();
    }

    public void ToReviewOutro() {
            introCamera.Priority = 0;
            gameplayCamera.Priority = 10;
            SkyboxBlender skyboxBlender = FindAnyObjectByType<SkyboxBlender>();
            playerData.CurrentGameplayState = GameplayState.Reviewing;
            playerData.CurrentGameState = GameState.Outro;
           
            if (skyboxBlender != null) {
                skyboxBlender.StartFade();

            }
            
    }

    IEnumerator ReloadEntriesCoroutine()
    {
        if (databaseManager == null)
        {
            databaseManager = FindAnyObjectByType<DatabaseManager>();
        }

        // Show any existing, previously-unloaded displays for this prompt immediately
        ShowDrawingDisplaysForPrompt(CurrentPromptIndex);

        if (databaseManager != null)
        {
            databaseManager.RefreshEntries();
        }

        // Wait briefly for remote reads to populate dataset and fire RemoteEntryLoaded events
        yield return new WaitForSeconds(0.5f);

        // Spawn any entries that are present in the dataset but not yet displayed
        LoadData();
    }

    private void OnRemoteEntryLoaded(Entry entry)
    {
        SpawnEntryDisplay(entry);
    }
    

    public void SkipToOutro()
    {
        playerData.PromptIndex = promptDatas.Length - 1; // Set to last prompt which is the review outro
        ToReviewOutro();
        StartCoroutine(PreloadAllPromptEntriesCoroutine());
        
    }

    private IEnumerator PreloadAllPromptEntriesCoroutine()
    {
        if (databaseManager == null)
        {
            databaseManager = FindAnyObjectByType<DatabaseManager>();
        }

        if (databaseManager == null || promptDatas == null || promptDatas.Length == 0)
        {
            ShowAllEntries();
            yield break;
        }

        // Prevent the DatabaseManager from forwarding RemoteEntryLoaded events
        // while we preload so new GameObjects aren't spawned/repositioned.
        databaseManager.SuppressRemoteNotifications = true;

        for (int i = 0; i < promptDatas.Length; i++)
        {
            PromptData promptData = promptDatas[i];
            if (promptData == null || string.IsNullOrWhiteSpace(promptData.DatabasePath))
            {
                continue;
            }

            Debug.Log($"Preloading prompt {i} from path '{promptData.DatabasePath}'");
            databaseManager.RefreshEntriesForPromptPath(promptData.DatabasePath);
            yield return new WaitForSeconds(0.5f);
        }

        // Stop suppressing after preload so subsequent remote entries behave normally.
        databaseManager.SuppressRemoteNotifications = false;

        // Ensure only the current prompt's displays are visible in the outro
        ShowDrawingDisplaysForPrompt(CurrentPromptIndex);
    }
    

  
}
