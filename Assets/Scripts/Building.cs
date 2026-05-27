using UnityEngine;
using TMPro;

public class Building : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;
    private TextMeshProUGUI buildingNameText;
    private float buildingNameLocalZOffset = -0.1f;
    private float buildingNameFadeDuration = 0.35f;

    public BuildingData BuildingData { get => buildingData; set => buildingData = value; }

    private CanvasGroup buildingNameCanvasGroup;
    private Coroutine fadeCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null && gameManager.BuildingNameCanvasPrefab != null)
        {
            Vector3 meshCenter = GetMeshCenter();
            GameObject canvasInstance = Instantiate(gameManager.BuildingNameCanvasPrefab, meshCenter, Quaternion.identity);
            canvasInstance.transform.SetParent(transform, true);
            canvasInstance.transform.localRotation = Quaternion.identity;
            canvasInstance.transform.localPosition += new Vector3(0f, 0.04f, buildingNameLocalZOffset);
            buildingNameText = canvasInstance.GetComponentInChildren<TextMeshProUGUI>();
            buildingNameCanvasGroup = canvasInstance.GetComponentInChildren<CanvasGroup>();
            if (buildingNameCanvasGroup == null)
            {
                buildingNameCanvasGroup = canvasInstance.AddComponent<CanvasGroup>();
            }

            buildingNameCanvasGroup.alpha = 0f;
            if (buildingNameText == null)
            {
                Debug.LogWarning("TextMeshProUGUI component not found in BuildingNameCanvasPrefab.");   
            }
        }
        else
        {
            Debug.LogWarning("GameManager or BuildingNameCanvasPrefab is not assigned.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCollisionEnter(Collision collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            ShowBuildingName();
        }
    }

    public void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.GetComponent<Player>() != null)
        {
            ClearBuildingName();
        }
    }

    public void DisplayHoverName()
    {
        ShowBuildingName();
    }

    private void ShowBuildingName()
    {
        if (buildingData != null)
        {
            if (buildingNameText != null)
            {
                buildingNameText.text = buildingData.BuildingName;
            }

            if (buildingNameCanvasGroup != null)
            {
                buildingNameCanvasGroup.alpha = 1f;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            Debug.Log("Building: " + buildingData.BuildingName);
        }
        else
        {
            Debug.LogWarning("BuildingData is not assigned for " + gameObject.name);
        }
    }

    private void ClearBuildingName()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeBuildingNameOut());
    }

    private System.Collections.IEnumerator FadeBuildingNameOut()
    {
        if (buildingNameCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = buildingNameCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < buildingNameFadeDuration)
        {
            elapsed += Time.deltaTime;
            buildingNameCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / buildingNameFadeDuration);
            yield return null;
        }

        buildingNameCanvasGroup.alpha = 0f;

        if (buildingNameText != null)
        {
            buildingNameText.text = string.Empty;
        }

        fadeCoroutine = null;
    }

    private Vector3 GetMeshCenter()
    {
        Renderer meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer != null)
        {
            return meshRenderer.bounds.center;
        }

        Collider meshCollider = GetComponentInChildren<Collider>();
        if (meshCollider != null)
        {
            return meshCollider.bounds.center;
        }

        return transform.position;
    }
}
