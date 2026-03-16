using UnityEngine;

public class DrawingDisplay : MonoBehaviour
{
    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void ShowDrawing(Texture2D texture)
    {
        rend.material.mainTexture = texture;
    }
}