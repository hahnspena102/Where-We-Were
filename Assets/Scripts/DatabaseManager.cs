using UnityEngine;
using System.IO;
using System;

public class DatabaseManager : MonoBehaviour
{
    public event Action<Entry> RemoteEntryLoaded;

    public TestEntryDatabase testEntryDatabase;
    public DatabaseLinker databaseLinker;

    private bool useLinkedDatabase;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (databaseLinker == null)
        {
            databaseLinker = GetComponent<DatabaseLinker>();
        }

        useLinkedDatabase = IsLinkedDatabaseAvailable();

        if (useLinkedDatabase)
        {
            Debug.Log("Web database available. New entries will be sent to the linked database.");
            databaseLinker.EntryLoadedFromDatabase += HandleEntryLoadedFromDatabase;
            databaseLinker.ReadEntriesFromDatabase();
        }
        else
        {
            Debug.Log("Linked web database unavailable in this runtime/configuration. Falling back to the test dataset.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
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
        string dataDir = Path.Combine(Application.dataPath, "Data");
        Directory.CreateDirectory(dataDir);
        string fileName = $"entry_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        string savePath = Path.Combine(dataDir, fileName);
        string assetPath = $"Assets/Data/{fileName}";
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
    
        int newId = testEntryDatabase.entries.Length > 0 ? testEntryDatabase.entries[testEntryDatabase.entries.Length - 1].id + 1 : 1;
        newEntry.id = newId;

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
        return testEntryDatabase.entries;
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

        AddOrReplaceEntryInDataset(remoteEntry);
        RemoteEntryLoaded?.Invoke(remoteEntry);
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
}
