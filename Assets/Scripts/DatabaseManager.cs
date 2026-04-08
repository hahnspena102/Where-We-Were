using UnityEngine;
using System.IO;
using System;

public class DatabaseManager : MonoBehaviour
{
    public TestEntryDatabase testEntryDatabase;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Entry AddEntry(Vector3 position, string answer, Texture2D processed)
    {
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

        Entry[] updatedEntries = new Entry[testEntryDatabase.entries.Length + 1];
        for (int i = 0; i < testEntryDatabase.entries.Length; i++)
        {
            updatedEntries[i] = testEntryDatabase.entries[i];
        }
        updatedEntries[updatedEntries.Length - 1] = newEntry;
        testEntryDatabase.entries = updatedEntries;
        return newEntry;
    }

    public Entry[] GetAllEntries()
    {
        return testEntryDatabase.entries;
    }
}
