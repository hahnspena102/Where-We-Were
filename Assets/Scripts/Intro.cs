using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;

public class Intro : MonoBehaviour
{
    [Header("Dialogue")]
    public string[] scriptLines;

    [Header("Input")]
    public InputActionReference continueAction;

    [Header("UI")]
    public TextMeshProUGUI textMeshPro;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float characterRevealDelay = 0.05f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private int currentLineIndex = 0;

    private bool isAnimatingLine = false;
    private bool isWaitingForContinue = false;
    private bool isScriptFinished = false;

    void Start()
    {
        textMeshPro.text = "";
        textMeshPro.maxVisibleCharacters = 0;

        Color c = textMeshPro.color;
        c.a = 0f;
        textMeshPro.color = c;

        ShowNextLine();
    }

    void Update()
    {
        if (isScriptFinished)
            return;

        if (continueAction.action.WasPressedThisFrame())
        {
            if (isAnimatingLine)
                return;

            if (isWaitingForContinue)
            {
                StartCoroutine(TransitionToNextLine());
            }
        }
    }

    private void ShowNextLine()
    {
        if (currentLineIndex >= scriptLines.Length)
        {
            isScriptFinished = true;
            SceneManager.LoadScene("MainScene");
            return;
        }

        StartCoroutine(AnimateLine(scriptLines[currentLineIndex]));
        currentLineIndex++;
    }

    private IEnumerator TransitionToNextLine()
    {
        isWaitingForContinue = false;

        yield return StartCoroutine(FadeOut());

        ShowNextLine();
    }

    private IEnumerator AnimateLine(string line)
    {
        isAnimatingLine = true;

        textMeshPro.text = line;
        textMeshPro.maxVisibleCharacters = 0;

        // Fade in
        Color c = textMeshPro.color;
        c.a = 0f;
        textMeshPro.color = c;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            textMeshPro.color = c;

            yield return null;
        }

        c.a = 1f;
        textMeshPro.color = c;

        // Character animation
        textMeshPro.ForceMeshUpdate();

        int totalCharacters = textMeshPro.textInfo.characterCount;

        for (int i = 0; i <= totalCharacters; i++)
        {
            textMeshPro.maxVisibleCharacters = i;
            yield return new WaitForSeconds(characterRevealDelay);
        }

        isAnimatingLine = false;
        isWaitingForContinue = true;
    }

    private IEnumerator FadeOut()
    {
        isAnimatingLine = true;

        Color c = textMeshPro.color;

        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;

            c.a = Mathf.Clamp01(1f - (elapsed / fadeOutDuration));
            textMeshPro.color = c;

            yield return null;
        }

        c.a = 0f;
        textMeshPro.color = c;

        isAnimatingLine = false;
    }
}