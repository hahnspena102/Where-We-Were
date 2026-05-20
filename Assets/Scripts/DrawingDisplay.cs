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