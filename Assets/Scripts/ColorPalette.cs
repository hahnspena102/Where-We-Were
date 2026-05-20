using UnityEngine;

public class ColorPalette : MonoBehaviour
{
    private GameManager gameManager;
    [SerializeField] private ColorButton[] colorButtons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        colorButtons = GetComponentsInChildren<ColorButton>();
        for (int i = 0; i < colorButtons.Length; i++)        {
            colorButtons[i].Color = gameManager.CurrentPromptData.ColorPalette[i];
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetColors()
    {
        for (int i = 0; i < colorButtons.Length; i++)
        {
            colorButtons[i].Color = gameManager.CurrentPromptData.ColorPalette[i];
        }
    }
}
