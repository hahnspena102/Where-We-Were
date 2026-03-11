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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvasGroup.alpha = 0;
        panelRect = GetComponent<RectTransform>();
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

    IEnumerator PromptCoroutine(string question)
    {
        promptText.text = question;
        
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

        Vector2 targetOffsetMin = new Vector2(40, 900);
        Vector2 targetOffsetMax = new Vector2(-1200, -40);

        float t = 0f;

        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, t / moveDuration);

            panelRect.offsetMin = Vector2.Lerp(startOffsetMin, targetOffsetMin, progress);
            panelRect.offsetMax = Vector2.Lerp(startOffsetMax, targetOffsetMax, progress);

            yield return null;
        }

        panelRect.offsetMin = targetOffsetMin;
        panelRect.offsetMax = targetOffsetMax;
    }
}