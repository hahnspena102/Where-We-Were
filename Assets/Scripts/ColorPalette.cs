using UnityEngine;

public class ColorPalette : MonoBehaviour
{
    [SerializeField] private PromptData promptData;
    [SerializeField] private ColorButton[] colorButtons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        colorButtons = GetComponentsInChildren<ColorButton>();
        for (int i = 0; i < colorButtons.Length; i++)        {
            colorButtons[i].Color = promptData.ColorPalette[i];
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
