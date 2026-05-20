using UnityEngine;

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
    

    public global::System.Int32 PromptIndex { get => promptIndex; set => promptIndex = value; }
    public GameState CurrentGameState { get => currentGameState; set => currentGameState = value; }
    public GameplayState CurrentGameplayState { get => currentGameplayState; set => currentGameplayState = value; }

    public void ResetData()
    {
        currentGameState = GameState.Intro;
        currentGameplayState = GameplayState.Exploring;
        promptIndex = 0;
    }
}
