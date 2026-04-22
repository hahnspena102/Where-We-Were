using UnityEngine;

public class DrawingDisplay : MonoBehaviour
{
    private Renderer rend;
    [SerializeField]private Entry entry;
    [SerializeField]private GameManager gameManager;

    public Entry Entry { get => entry; set => entry = value; }

    void Awake()
    {
        rend = GetComponent<Renderer>();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    void Update()
    {
        if (entry != null && entry.sprite != null)
        {
            rend.material.mainTexture = entry.sprite.texture;
        }

        if (gameManager.CurrentState == GameState.Reviewing)
        {
            rend.enabled = true;
        }
        else
        {
            rend.enabled = false;
        }
        
    }



    
}