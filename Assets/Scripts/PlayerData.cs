using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum GameState
{
    Intro,
    Gameplay,
    Transition,
    Outro
}

public enum GameplayState
{
    Exploring,
    Prompting,
    Answering,
    Drawing,
    Explaining,
    Reviewing

}

[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    [SerializeField] private GameState currentGameState = GameState.Intro;
    [SerializeField] private GameplayState currentGameplayState = GameplayState.Exploring;
    [SerializeField] private int promptIndex = 0;
    [SerializeField] private List<Entry> entriesRead;
    

    public global::System.Int32 PromptIndex { get => promptIndex; set => promptIndex = value; }
    public GameState CurrentGameState { get => currentGameState; set => currentGameState = value; }
    public GameplayState CurrentGameplayState { get => currentGameplayState; set => currentGameplayState = value; }
    public List<Entry> EntriesRead { get => entriesRead; set => entriesRead = value; }

    public void ResetData()
    {
        currentGameState = GameState.Intro;
        currentGameplayState = GameplayState.Exploring;
        promptIndex = 0;
        entriesRead = new List<Entry>();
    }

    public void AddEntryRead(Entry entry)
    {
        if (!entriesRead.Contains(entry))
        {
            entriesRead.Add(entry);
        }
    }

    public int GetNumEntriesReadForPrompt(int promptId)
    {
        return entriesRead.Count(entry => entry.promt_id == promptId);
    }
}
