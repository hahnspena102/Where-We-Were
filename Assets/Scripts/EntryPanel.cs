using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class EntryPanel : MonoBehaviour
{
    private RectTransform panelRect;
    private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private Button nextButton;
    private GameManager gameManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        panelRect = GetComponent<RectTransform>();

        gameManager = FindAnyObjectByType<GameManager>();
        //StartCoroutine(PromptCoroutine("Recall a place you felt alone."));

    }

    public void ResetPanel()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        promptText.text = "";
        answerInputField.text = "";
        nextButton.interactable = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!EnoughTextEntered())
        {
            nextButton.interactable = false;
        }
        else
        {
            nextButton.interactable = true;
        }        
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
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        while(canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime / fadeDuration;
            yield return null;
        }

    }

    IEnumerator PromptEntry(string question)
    {
        promptText.text = question;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        while(canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime / fadeDuration;
            yield return null;
        }
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        answerInputField.interactable = true;
        answerInputField.ActivateInputField();
        nextButton.interactable = EnoughTextEntered();
    }

    public void NextPage()
    {
        if (!EnoughTextEntered())
        {
            Debug.Log("Not enough text entered, not proceeding to next page.");
            return;
        }

        gameManager.NextPage();
    }

    public void PreviousPage()
    {

        gameManager.PreviousPage();
    }

    public string GetEntryText()
    {
        return answerInputField.text;
    }

    public bool EnoughTextEntered()
    {
        int minCharacters = 10; // Set a minimum character count for the answer
        return !string.IsNullOrWhiteSpace(answerInputField.text) && answerInputField.text.Length >= minCharacters;
    }
}