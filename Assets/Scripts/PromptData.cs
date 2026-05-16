using UnityEngine;

enum PromptId
{
    Star,
    Flower,
    House,
}

[CreateAssetMenu(fileName = "PromptData", menuName = "Scriptable Objects/PromptData")]
public class PromptData : ScriptableObject
{
    [SerializeField] private string promptText;
    [SerializeField] private string drawInstruction;
    [SerializeField] private PromptId promptId;
    [SerializeField] private string databasePath;
    [SerializeField] private Color[] colorPalette;

    public Color[] ColorPalette { get => colorPalette; set => colorPalette = value; }
    public global::System.String PromptText { get => promptText; set => promptText = value; }
    public global::System.String DrawInstruction { get => drawInstruction; set => drawInstruction = value; }
    public global::System.String DatabasePath { get => databasePath; set => databasePath = value; }
}
