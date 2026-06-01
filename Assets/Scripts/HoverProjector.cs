using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HoverProjector : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float hoverOffset = 0.4f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip chargeSound;
    [SerializeField]private AudioClip finishSound;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Slider slider;
    private Player player;
    private GameManager gameManager;
    private bool finishSoundPlayed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = FindAnyObjectByType<Player>();
        canvas = GetComponentInChildren<Canvas>();
        canvasGroup = GetComponentInChildren<CanvasGroup>();
        slider = GetComponent<Slider>();

        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5000;
        }

        canvasGroup.alpha = 0;
        gameManager = FindAnyObjectByType<GameManager>();
    
    }
    

    // Update is called once per frame
  public Vector3 HoverProject()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        slider.value = player.GetHoldPercentage();

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            float upDot = Vector3.Dot(hit.normal, Vector3.up);
            bool mostlyUp = upDot > 0.7f;

            float distance = Vector3.Distance(player.transform.position, hit.point);
            bool withinRange = distance <= 64f;

            if (mostlyUp && withinRange)
            {
                canvasGroup.alpha = 1;

                transform.position = hit.point + hit.normal * (hoverOffset + 0.3f);
                transform.rotation = Quaternion.LookRotation(hit.normal);

                return hit.point;
            }
            else
            {
                canvasGroup.alpha = 0;
            }
        }
        else
        {
            canvasGroup.alpha = 0;
        }

        return Vector3.zero;
    }

    public void ShowHover()
    {
        gameObject.SetActive(true);
    }
    
    public void HideHover()
    {
        gameObject.SetActive(false);
        canvasGroup.alpha = 0;
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        finishSoundPlayed = false;
    }

    void Update()
    {
        if (player == null || audioSource == null)
        {
            return;
        }


        float holdPercentage = player.GetHoldPercentage();
    

        if (holdPercentage >= 1f)
        {
            if (!finishSoundPlayed)
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                if (finishSound != null)
                {
                    audioSource.PlayOneShot(finishSound);
                }

                finishSoundPlayed = true;
            }
        }
        else if (holdPercentage > 0.05f)
        {
            finishSoundPlayed = false;

            if (!audioSource.isPlaying && chargeSound != null)
            {
                audioSource.PlayOneShot(chargeSound);
            }
        }
        else
        {
            if (audioSource.isPlaying && !finishSoundPlayed)
            {
                audioSource.Stop();
            }
        }
    
    }

}
