using UnityEngine;
using TMPro;
using System.Collections;

public class EntryPanel : MonoBehaviour
{
    private RectTransform panelRect;
    private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private float fadeDuration = 0.5f;
    private GameManager gameManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelRect = GetComponent<RectTransform>();

        gameManager = FindFirstObjectByType<GameManager>();
        //StartCoroutine(PromptCoroutine("Recall a place you felt alone."));

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartEntry(string question)
    {
        StartCoroutine(PromptEntry(question));
    }

    public void HideEntry()
    {
        StartCoroutine(HideEntryCoroutine());
    }

    IEnumerator HideEntryCoroutine()
    {
        while(canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime / fadeDuration;
            yield return null;
        }
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    IEnumerator PromptEntry(string question)
    {
        promptText.text = question;
        canvasGroup.interactable = false;
        while(canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime / fadeDuration;
            yield return null;
        }
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void NextPage()
    {
        gameManager.NextPage();
    }

    public void PreviousPage()
    {
        gameManager.PreviousPage();
    }
}