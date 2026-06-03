using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;

public class TextDisplay : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private ScriptData scriptData;

    [Header("Input")]
    public InputActionReference continueAction;

    [Header("UI")]
    public TextMeshProUGUI textMeshPro;
    public TextMeshProUGUI enterToContinueText;
    public CanvasGroup bgGroup;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float characterRevealDelay = 0.05f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private bool enterToContinue = true;
    [SerializeField] private float displayedTextDuration = 0f;
    [SerializeField] private float enterFadeDuration = 0.3f;
    
    private float displayedTextTimer = 0f;
    private int currentLineIndex = 0;

    private bool isAnimatingLine = false;
    private bool isWaitingForContinue = false;
    private bool isFading = false;
    private bool isScriptFinished = false;
    private Coroutine currentLineCoroutine;
    private bool isIntro = true;
    private Coroutine enterFadeCoroutine;
    

    public ScriptData ScriptData { get => scriptData; set => scriptData = value; }

    void Start()
    {
        textMeshPro.text = "";
        textMeshPro.maxVisibleCharacters = 0;
        ResetDisplay(scriptData, true);
        if (enterToContinueText != null)
        {
            Color ec = enterToContinueText.color;
            ec.a = 0f;
            enterToContinueText.color = ec;
            enterToContinueText.gameObject.SetActive(false);
        }
    }


    void Update()
    {
       
        // Auto-advance mode: only start the timer once the full line is visible and waiting for continue.
        if (!enterToContinue)
        {
            if (isScriptFinished)
            {
                bgGroup.alpha = Mathf.MoveTowards(bgGroup.alpha, 0f, Time.deltaTime / fadeOutDuration);
                return;
            }

            bool isLineFullyDisplayed = !isAnimatingLine && isWaitingForContinue && textMeshPro.textInfo.characterCount > 0 && textMeshPro.maxVisibleCharacters >= textMeshPro.textInfo.characterCount;
            if (isLineFullyDisplayed)
            {
                displayedTextTimer += Time.deltaTime;
                if (displayedTextTimer >= displayedTextDuration)
                {
                    displayedTextTimer = 0f;
                    // If this was the last line, fade out then finish; otherwise show next line.
                    if (scriptData != null && currentLineIndex >= scriptData.Lines.Length)
                    {
                        if (currentLineCoroutine == null)
                            currentLineCoroutine = StartCoroutine(FadeOutAndFinish());
                    }
                    else
                    {
                        ShowNextLine();
                    }
                }
            } else {
                bgGroup.alpha = Mathf.MoveTowards(bgGroup.alpha, 1f, Time.deltaTime / fadeDuration);
            }
            return;
        }
        else
        {
            if (continueAction.action.WasPressedThisFrame())
            {
                if (isAnimatingLine)
                {
                    if (currentLineCoroutine != null)
                    {
                        StopCoroutine(currentLineCoroutine);
                        currentLineCoroutine = null;
                    }
                    textMeshPro.maxVisibleCharacters = textMeshPro.textInfo.characterCount;
                    isAnimatingLine = false;
                    isWaitingForContinue = true;
                    if (enterToContinueText != null && enterToContinue)
                        StartEnterFade(true);
                    return;
                }

                if (isFading)
                {
                    // If a fade is in progress, cancel it and immediately show the next line.
                    if (currentLineCoroutine != null)
                    {
                        StopCoroutine(currentLineCoroutine);
                        currentLineCoroutine = null;
                    }
                    isFading = false;
                    isAnimatingLine = false;
                    isWaitingForContinue = false;
                    if (enterToContinueText != null)
                        StartEnterFade(false);
                    ShowNextLine();
                    return;
                }

                if (isWaitingForContinue)
                {
                    if (currentLineCoroutine == null)
                    {
                        currentLineCoroutine = StartCoroutine(TransitionToNextLine());
                    }
                }
            }
        }
    }

    public void ShowNextLine()
    {
        Debug.Log("isIntro: " + isIntro + ", currentLineIndex: " + currentLineIndex + ", totalLines: " + (scriptData != null ? scriptData.Lines.Length : "null"));
        if (currentLineIndex >= scriptData.Lines.Length)
        {
            isScriptFinished = true;
            if (isIntro) {
                Debug.Log("Intro script finished. Transitioning to gameplay.");
                GameManager gameManager = FindAnyObjectByType<GameManager>();
                if (gameManager != null)            {
                    if (gameManager.PlayerData.CurrentGameState == GameState.Outro)
                    {
                        gameManager.ToReviewOutro();
                    } else {
                        gameManager.SwitchGameState(GameState.Gameplay);
                    }
                }
            }
            
            Debug.Log("All lines displayed. Transitioning to review.");
            return;
        }
        if (currentLineCoroutine != null)
        {
            StopCoroutine(currentLineCoroutine);
            currentLineCoroutine = null;
        }

        currentLineCoroutine = StartCoroutine(AnimateLine(scriptData.Lines[currentLineIndex]));
        currentLineIndex++;
    }

    private IEnumerator TransitionToNextLine()
    {
        isWaitingForContinue = false;
        if (enterToContinueText != null)
            StartEnterFade(false);

        yield return FadeOut();

        ShowNextLine();
    }

    private IEnumerator AnimateLine(string line)
    {
        Debug.Log("Animating line: " + line);
        isAnimatingLine = true;

        if (enterToContinueText != null)
            StartEnterFade(false);

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
        currentLineCoroutine = null;
        if (enterToContinueText != null && enterToContinue)
            StartEnterFade(true);
    }

    private IEnumerator FadeOut()
    {
        isFading = true;

        if (enterToContinueText != null)
            StartEnterFade(false);

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
        isFading = false;
        isAnimatingLine = false;
        currentLineCoroutine = null;
    }

    private IEnumerator FadeOutAndFinish()
    {
        yield return FadeOut();

        isScriptFinished = true;
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.PlayerData.CurrentGameplayState = GameplayState.Reviewing;
        }
        currentLineCoroutine = null;
    }

    public void ResetDisplay(ScriptData newScriptData, bool resetAsIntro = false)
    {
        if (resetAsIntro)
        {
            Debug.Log("Setting enterToContinue to true for intro script.");
            enterToContinue = true;
            displayedTextDuration = 0f;
            isIntro = true;
        
        }
        else
        {
            Debug.Log("Setting enterToContinue to false for after draw script.");
            enterToContinue = false;
            displayedTextDuration = 5f;
            isIntro = false;
            
        }

        scriptData = newScriptData;
        currentLineIndex = 0;
        isAnimatingLine = false;
        isWaitingForContinue = false;
        isFading = false;
        isScriptFinished = false;

        if (currentLineCoroutine != null)
        {
            StopCoroutine(currentLineCoroutine);
            currentLineCoroutine = null;
        }

        textMeshPro.text = "";
        textMeshPro.maxVisibleCharacters = 0;

        Color c = textMeshPro.color;
        c.a = 0f;
        textMeshPro.color = c;

        if (enterToContinueText != null)
            StartEnterFade(false);

        ShowNextLine();
    }

    public void ClearTextMesh()
    {
        textMeshPro.text = "";
        textMeshPro.maxVisibleCharacters = 0;
    }

    private void StartEnterFade(bool show)
    {
        if (enterToContinueText == null) return;

        if (enterFadeCoroutine != null)
        {
            StopCoroutine(enterFadeCoroutine);
            enterFadeCoroutine = null;
        }
        enterFadeCoroutine = StartCoroutine(FadeEnterRoutine(show));
    }

    private IEnumerator FadeEnterRoutine(bool show)
    {
        if (enterToContinueText == null) yield break;

        float elapsed = 0f;
        Color c = enterToContinueText.color;
        float start = c.a;
        float end = show ? 0.1f : 0f;

        if (show)
            enterToContinueText.gameObject.SetActive(true);

        while (elapsed < enterFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / enterFadeDuration);
            c.a = Mathf.Lerp(start, end, t);
            enterToContinueText.color = c;
            yield return null;
        }

        c.a = end;
        enterToContinueText.color = c;
        if (!show)
            enterToContinueText.gameObject.SetActive(false);

        enterFadeCoroutine = null;
    }

    public void ClearText()
    {
        if (currentLineCoroutine != null)
        {
            StopCoroutine(currentLineCoroutine);
            currentLineCoroutine = null;
        }
        textMeshPro.text = "";
        textMeshPro.maxVisibleCharacters = 0;
        Color c = textMeshPro.color;
        c.a = 0f;
        textMeshPro.color = c;

        if (enterToContinueText != null)
            StartEnterFade(false);
    }
}