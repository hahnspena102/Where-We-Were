using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    [SerializeField]private Color color;
    [SerializeField]private Image fillImage;
    private DrawPanel drawPanel;

    public Color Color { get => color; set => color = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        drawPanel = FindAnyObjectByType<DrawPanel>();
    }

    // Update is called once per frame
    void Update()
    {
        fillImage.color = color;
    }
    
    public void OnButtonClicked()
    {
        Debug.Log("Color button clicked: " + color);
        drawPanel.CurrentColor = color;
    }
}
