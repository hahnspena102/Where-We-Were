using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    [SerializeField] private int promptIndex = 0;

    public global::System.Int32 PromptIndex { get => promptIndex; set => promptIndex = value; }
}
