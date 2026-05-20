using UnityEngine;

public class MenuScene : MonoBehaviour
{
    public PlayerData playerData;
    void Awake()
    {
        playerData.ResetData();
        if (playerData == null)
        {
            Debug.LogError("PlayerData is not assigned in the MenuScene.");
            return;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    }
}
