using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public class DatabaseManager : MonoBehaviour
{
    public event Action<Entry> RemoteEntryLoaded;

    public TestEntryDatabase testEntryDatabase;
    public DatabaseLinker databaseLinker;

    private bool useLinkedDatabase;
    private GameManager gameManager;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        if (databaseLinker == null)
        {
            databaseLinker = GetComponent<DatabaseLinker>();
        }

        useLinkedDatabase = false;
        TryEnableLinkedDatabase();

        if (!useLinkedDatabase)
        {
            Debug.Log("Linked web database not ready yet. Waiting for the current prompt path before reading remote data.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!useLinkedDatabase)
        {
            TryEnableLinkedDatabase();
        }
    }

    private void OnDestroy()
    {
        if (databaseLinker != null)
        {
            databaseLinker.EntryLoadedFromDatabase -= HandleEntryLoadedFromDatabase;
            databaseLinker.StopReadingEntriesFromDatabase();
        }
    }

    public Entry AddEntry(Vector3 position, string answer, Texture2D processed)
    {
        //cap answer at 500 characters to prevent excessively long entries
        if (answer.Length > 500)        {
            answer = answer.Substring(0, 500);
        }

        // package entry
        Entry newEntry = new Entry
        {
            position = position,
            answer = answer,
            dataPosted = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            sprite = null 
        };

        byte[] imgBytes = processed.EncodeToPNG();
        string dataDir = ResolveLocalCacheDirectory();
        Directory.CreateDirectory(dataDir);
        string fileName = $"entry_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        string savePath = Path.Combine(dataDir, fileName);
        string assetPath = GetAssetRelativePath(dataDir, fileName);
        File.WriteAllBytes(savePath, imgBytes);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
        if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
        {
            importer.textureType = UnityEditor.TextureImporterType.Sprite;
            importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        newEntry.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#endif

        if (newEntry.sprite == null)
        {
            Texture2D imgTexture = new Texture2D(processed.width, processed.height);
            imgTexture.LoadImage(imgBytes);
            newEntry.sprite = Sprite.Create(imgTexture, new Rect(0, 0, imgTexture.width, imgTexture.height), new Vector2(0.5f, 0.5f));
        }

        Debug.Log("Saved PNG to: " + savePath + " | Sprite: " + newEntry.sprite);
    
        // Generate globally unique ID by finding max ID across all entries, not just current prompt
        int newId = 1;
        if (testEntryDatabase != null && testEntryDatabase.entries != null && testEntryDatabase.entries.Length > 0)
        {
            int maxId = 0;
            for (int i = 0; i < testEntryDatabase.entries.Length; i++)
            {
                if (testEntryDatabase.entries[i] != null && testEntryDatabase.entries[i].id > maxId)
                {
                    maxId = testEntryDatabase.entries[i].id;
                }
            }
            newId = maxId + 1;
        }
        newEntry.id = newId;
        newEntry.promt_id = GetCurrentPromptIndex();

        if (useLinkedDatabase)
        {
            try
            {
                databaseLinker.WriteEntryToDatabase(newEntry, imgBytes, fileName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Linked database write failed. Falling back to test dataset. {ex.Message}");
                useLinkedDatabase = false;
            }
        }

        // Keep a local in-memory copy so current runtime systems can still read entries.
        AddEntryToTestDataset(newEntry);
        return newEntry;
    }

    public Entry[] GetAllEntries()
    {
        return GetCurrentPromptEntries();
    }

    private bool IsLinkedDatabaseAvailable()
    {
        if (databaseLinker == null)
        {
            Debug.LogWarning("DatabaseLinker reference is missing.");
            return false;
        }

        if (!databaseLinker.HasDatabasePathConfigured())
        {
            Debug.LogWarning("DatabaseLinker has no database path configured.");
            return false;
        }

        bool isRuntimeAvailable = databaseLinker.IsRuntimeFirebaseAvailable();
        if (!isRuntimeAvailable)
        {
            Debug.LogWarning($"Firebase runtime unavailable on platform '{Application.platform}'. Use a WebGL player build.");
        }

        return isRuntimeAvailable;
    }

    private void TryEnableLinkedDatabase()
    {
        if (useLinkedDatabase)
        {
            return;
        }

        if (databaseLinker == null)
        {
            return;
        }

        if (!databaseLinker.IsRuntimeFirebaseAvailable())
        {
            return;
        }

        if (!databaseLinker.HasDatabasePathConfigured())
        {
            return;
        }

        useLinkedDatabase = true;
        Debug.Log("Web database is ready. New entries will be sent to the linked database.");

        databaseLinker.EntryLoadedFromDatabase -= HandleEntryLoadedFromDatabase;
        databaseLinker.EntryLoadedFromDatabase += HandleEntryLoadedFromDatabase;
        databaseLinker.ReadEntriesFromDatabase();
    }

    private void AddEntryToTestDataset(Entry newEntry)
    {
        if (testEntryDatabase == null)
        {
            Debug.LogWarning("No test dataset assigned; entry is not cached locally.");
            return;
        }

        Entry[] existingEntries = testEntryDatabase.entries ?? Array.Empty<Entry>();
        Entry[] updatedEntries = new Entry[existingEntries.Length + 1];

        for (int i = 0; i < existingEntries.Length; i++)
        {
            updatedEntries[i] = existingEntries[i];
        }

        updatedEntries[updatedEntries.Length - 1] = newEntry;
        testEntryDatabase.entries = updatedEntries;
    }

    private void HandleEntryLoadedFromDatabase(Entry remoteEntry)
    {
        if (remoteEntry == null)
        {
            return;
        }

        Debug.Log($"DatabaseManager.HandleEntryLoadedFromDatabase: id={remoteEntry.id} promt_id={remoteEntry.promt_id} position={remoteEntry.position} sprite={(remoteEntry.sprite!=null?"yes":"no")}");

        // Always add/replace in the local dataset. Let listeners decide whether to spawn.
        AddOrReplaceEntryInDataset(remoteEntry);
        RemoteEntryLoaded?.Invoke(remoteEntry);
    }

    // Request a refresh from the linked database. For local test dataset this is a no-op.
    public void RefreshEntries()
    {
        TryEnableLinkedDatabase();

        if (useLinkedDatabase && databaseLinker != null)
        {
            // Ensure the databaseLinker is listening to the current prompt's entries path
            string promptPath = null;
            if (gameManager != null)
            {
                int promptIndex = gameManager.CurrentPromptIndex;
                PromptData[] promptDatas = gameManager.PromptDatas;
                if (promptDatas != null && promptIndex >= 0 && promptIndex < promptDatas.Length && promptDatas[promptIndex] != null)
                {
                    promptPath = promptDatas[promptIndex].DatabasePath;
                }
                else if (gameManager.CurrentPromptData != null)
                {
                    promptPath = gameManager.CurrentPromptData.DatabasePath;
                }
            }

            if (!string.IsNullOrWhiteSpace(promptPath))
            {
                databaseLinker.SetEntriesPath(promptPath);
                databaseLinker.ReadEntriesFromDatabase();
            }
            else
            {
                Debug.LogWarning("RefreshEntries: prompt path not configured, skipping ReadEntriesFromDatabase().");
            }
        }
    }

    public void ClearCachedEntriesForCurrentPrompt()
    {
        if (testEntryDatabase == null)
        {
            return;
        }

        int currentPromptIndex = GetCurrentPromptIndex();
        Entry[] existingEntries = testEntryDatabase.entries ?? Array.Empty<Entry>();
        List<Entry> retainedEntries = new List<Entry>(existingEntries.Length);

        for (int i = 0; i < existingEntries.Length; i++)
        {
            Entry entry = existingEntries[i];
            if (entry == null || entry.promt_id != currentPromptIndex)
            {
                continue;
            }

            retainedEntries.Add(entry);
        }

        testEntryDatabase.entries = retainedEntries.ToArray();
        Debug.Log($"Cleared cached entries for prompt {currentPromptIndex}; retained {retainedEntries.Count} entries.");
    }

    private void AddOrReplaceEntryInDataset(Entry newEntry)
    {
        if (testEntryDatabase == null)
        {
            return;
        }

        Entry[] existingEntries = testEntryDatabase.entries ?? Array.Empty<Entry>();
        for (int i = 0; i < existingEntries.Length; i++)
        {
            if (existingEntries[i] != null && existingEntries[i].id == newEntry.id)
            {
                existingEntries[i] = newEntry;
                testEntryDatabase.entries = existingEntries;
                return;
            }
        }

        AddEntryToTestDataset(newEntry);
    }

    private Entry[] GetCurrentPromptEntries()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        if (testEntryDatabase == null)
        {
            return Array.Empty<Entry>();
        }

        int currentPromptIndex = GetCurrentPromptIndex();
        Entry[] allEntries = testEntryDatabase.entries ?? Array.Empty<Entry>();
        int matchCount = 0;

        for (int i = 0; i < allEntries.Length; i++)
        {
            if (allEntries[i] != null && allEntries[i].promt_id == currentPromptIndex)
            {
                matchCount++;
            }
        }

        Entry[] currentPromptEntries = new Entry[matchCount];
        int writeIndex = 0;
        for (int i = 0; i < allEntries.Length; i++)
        {
            if (allEntries[i] != null && allEntries[i].promt_id == currentPromptIndex)
            {
                currentPromptEntries[writeIndex++] = allEntries[i];
            }
        }

        return currentPromptEntries;
    }

    private int GetCurrentPromptIndex()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        return gameManager != null ? gameManager.CurrentPromptIndex : 0;
    }

    private string ResolveLocalCacheDirectory()
    {
        string promptPath = null;

        if (gameManager != null)
        {
            int promptIndex = gameManager.CurrentPromptIndex;
            PromptData[] promptDatas = gameManager.PromptDatas;

            if (promptDatas != null && promptIndex >= 0 && promptIndex < promptDatas.Length && promptDatas[promptIndex] != null)
            {
                promptPath = promptDatas[promptIndex].DatabasePath;
            }
            else if (gameManager.CurrentPromptData != null)
            {
                promptPath = gameManager.CurrentPromptData.DatabasePath;
            }
        }

        if (string.IsNullOrWhiteSpace(promptPath))
        {
            return Path.Combine(Application.dataPath, "Data");
        }

        string safePromptPath = promptPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(Application.dataPath, "Data", safePromptPath);
    }

    private string GetAssetRelativePath(string dataDir, string fileName)
    {
        string relativePath = dataDir.Replace(Application.dataPath, "Assets");
        return Path.Combine(relativePath, fileName).Replace('\\', '/');
    }
}
