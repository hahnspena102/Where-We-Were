using UnityEngine;
using TMPro;
using System.Collections;

public class PromptPanel : MonoBehaviour
{
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private float moveDuration = 5f;
    [SerializeField] private Vector2 topRightOffset = new Vector2(-20, -20);
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float showDuration = 5.0f;
    [SerializeField] private AudioClip crumbleSound;
    [SerializeField]private AudioClip revealSound;
    
    [SerializeField] private AudioSource audioSource1;
    [SerializeField] private AudioSource audioSource2;
    private bool hasDisplayedPrompt = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvasGroup.alpha = 0;


        //panelRect = GetComponent<RectTransform>();
        //StartCoroutine(PromptCoroutine("Recall a place you felt alone."));

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartPrompt(string question)
    {
        StartCoroutine(PromptCoroutine(question));
    }

    public void HidePrompt()
    {
        StartCoroutine(HidePromptCoroutine());
    }

    IEnumerator HidePromptCoroutine()
    {
        while(canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime / fadeDuration;
            yield return null;
        }
    }

    IEnumerator PromptCoroutine(string question)
    {
        if (hasDisplayedPrompt)
        {
            yield break; 
        }
        hasDisplayedPrompt = true; 
        promptText.text = question;

        if (audioSource2 != null && revealSound != null)
        {
            audioSource2.PlayOneShot(revealSound);
        }
        
        while(canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime / fadeDuration;
            yield return null;
        }
        yield return new WaitForSeconds(showDuration);

        yield return StartCoroutine(MovePanelTopRight());
    }

  IEnumerator MovePanelTopRight()
    {
        Vector2 startOffsetMin = panelRect.offsetMin;
        Vector2 startOffsetMax = panelRect.offsetMax;

        Vector2 targetOffsetMin = new Vector2(80, 700);
        Vector2 targetOffsetMax = new Vector2(-1000, -80);

        float t = 0f;
        PlayCrumbleSound();

        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, t / moveDuration);

            panelRect.offsetMin = Vector2.Lerp(startOffsetMin, targetOffsetMin, progress);
            panelRect.offsetMax = Vector2.Lerp(startOffsetMax, targetOffsetMax, progress);

            yield return null;
        }
        // fade audio out
        if (audioSource1 != null)
        {
            float startVolume = audioSource1.volume;
            float fadeOutTime = 0.1f; 
            float fadeOutTimer = 0f;

            while (fadeOutTimer < fadeOutTime)
            {
                fadeOutTimer += Time.deltaTime;
                audioSource1.volume = Mathf.Lerp(startVolume, 0, fadeOutTimer / fadeOutTime);
                yield return null;
            }
            audioSource1.Stop();
            audioSource1.volume = startVolume; 
        }
    

        panelRect.offsetMin = targetOffsetMin;
        panelRect.offsetMax = targetOffsetMax;
    }

    public void PlayCrumbleSound()
    {
        if (audioSource1 != null && crumbleSound != null)
        {
            audioSource1.PlayOneShot(crumbleSound);
        }
    }
}