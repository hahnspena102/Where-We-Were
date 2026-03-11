using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private float elapsedWorldTime;
    private PromptPanel promptPanel;
    private bool startPrompt;
    private float timeUntilPrompt = 5f;

    void Start()
    {
        promptPanel = FindFirstObjectByType<PromptPanel>();
    }
    void Awake() {
        int countLoaded = SceneManager.sceneCount;
        Scene[] loadedScenes = new Scene[countLoaded];

        for (int i = 0; i < countLoaded; i++)
        {
            loadedScenes[i] = SceneManager.GetSceneAt(i);
            if (loadedScenes[i].name == "UIScene")
            {
                return;
            }
        }
        
        SceneManager.LoadSceneAsync("UIScene", LoadSceneMode.Additive);

    }

    void Update()
    {
        elapsedWorldTime += Time.deltaTime;

        if (elapsedWorldTime >= timeUntilPrompt && startPrompt == false) 
        {
            promptPanel.StartPrompt("Recall a place you felt alone.");
            startPrompt = true;
        }
    }
}
