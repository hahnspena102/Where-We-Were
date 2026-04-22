using UnityEngine;

public class ClearButton : MonoBehaviour
{
    public void OnClearButtonClicked()
    {
        DrawPanel drawPanel = FindFirstObjectByType<DrawPanel>();
        if (drawPanel != null)
        {
            drawPanel.ClearCanvas();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
