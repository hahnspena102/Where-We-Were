using UnityEngine;

enum PromptId
{
    Star,
    Flower,
    Anything,
}

[CreateAssetMenu(fileName = "PromptData", menuName = "Scriptable Objects/PromptData")]
public class PromptData : ScriptableObject
{
    [SerializeField] private string promptText;
    [SerializeField] private string typeInstruction;
    [SerializeField] private string drawInstruction;
    [SerializeField] private PromptId promptId;
    [SerializeField] private string databasePath;
    [SerializeField] private ScriptData afterDrawScript;
    [SerializeField] private Color[] colorPalette;
    [SerializeField] private bool isOnGround;

    public Color[] ColorPalette { get => colorPalette; set => colorPalette = value; }
    public global::System.String PromptText { get => promptText; set => promptText = value; }
    public global::System.String DrawInstruction { get => drawInstruction; set => drawInstruction = value; }
    public global::System.String DatabasePath { get => databasePath; set => databasePath = value; }
    public ScriptData AfterDrawScript { get => afterDrawScript; set => afterDrawScript = value; }
    public global::System.String TypeInstruction { get => typeInstruction; set => typeInstruction = value; }
    public global::System.Boolean IsOnGround { get => isOnGround; set => isOnGround = value; }
}
