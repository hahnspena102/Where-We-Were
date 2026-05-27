using UnityEngine;

public class DrawingDisplay : MonoBehaviour
{
    private Renderer rend;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    [SerializeField]private Entry entry;
    [SerializeField]private GameManager gameManager;

    public Entry Entry { get => entry; set => entry = value; }

    void Awake()
    {
        rend = GetComponent<Renderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        if (meshFilter != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        gameManager = FindAnyObjectByType<GameManager>();
    }

    void Update()
    {
        if (entry != null && entry.sprite != null)
        {
            rend.material.mainTexture = entry.sprite.texture;
        }

        if (gameManager.PlayerData.CurrentGameplayState == GameplayState.Reviewing || gameManager.PlayerData.CurrentGameplayState == GameplayState.Explaining)
        {
            rend.enabled = true;
        }
        else
        {
            rend.enabled = false;
        }
        
    }



    
}