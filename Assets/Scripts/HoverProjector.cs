using UnityEngine;
using UnityEngine.InputSystem;

public class HoverProjector : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float hoverOffset = 0.1f;

    [SerializeField] private SpriteRenderer spriteRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    public Vector3 HoverProject()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if surface is mostly facing upward
            float upDot = Vector3.Dot(hit.normal, Vector3.up);
            bool mostlyUp = upDot > 0.7f; // adjust threshold if needed

            // Check if within placement radius
            float distance = Vector3.Distance(transform.position, hit.point);
            bool withinRange = distance <= 64f;

            if (mostlyUp && withinRange)
            {
                gameObject.SetActive(true);
                spriteRenderer.enabled = true;

                transform.position = hit.point + hit.normal + new Vector3(0, hoverOffset, 0);
                transform.rotation = Quaternion.LookRotation(hit.normal);
            }
            else
            {
                spriteRenderer.enabled = false;
                return Vector3.zero;
            }
        }
        else
        {
            gameObject.SetActive(false);
            spriteRenderer.enabled = false;
            return Vector3.zero;
        }
        return transform.position;

    }

    public void HideHover()
    {
        gameObject.SetActive(false);
        spriteRenderer.enabled = false;
    }

}
