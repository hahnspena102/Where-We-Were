using UnityEngine;

[CreateAssetMenu(fileName = "ScriptData", menuName = "Scriptable Objects/ScriptData")]
public class ScriptData : ScriptableObject
{
    [SerializeField]private string[] lines;

    public global::System.String[] Lines { get => lines; set => lines = value; }
}
