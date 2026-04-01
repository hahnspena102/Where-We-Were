using UnityEngine;
using UnityEngine.InputSystem;

public class HoverProjector : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float hoverOffset = 0.4f;

    [SerializeField] private SpriteRenderer spriteRenderer;
    private Player player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = FindFirstObjectByType<Player>();
    }

    // Update is called once per frame
  public Vector3 HoverProject()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            float upDot = Vector3.Dot(hit.normal, Vector3.up);
            bool mostlyUp = upDot > 0.7f;

            float distance = Vector3.Distance(player.transform.position, hit.point);
            bool withinRange = distance <= 64f;

            if (mostlyUp && withinRange)
            {
                spriteRenderer.enabled = true;

                transform.position = hit.point + hit.normal + new Vector3(0, hoverOffset, 0);
                transform.rotation = Quaternion.LookRotation(hit.normal);

                return transform.position;
            }
            else
            {
                spriteRenderer.enabled = false;
            }
        }
        else
        {
            spriteRenderer.enabled = false;
        }

        return Vector3.zero;
    }

    public void HideHover()
    {
        gameObject.SetActive(false);
        spriteRenderer.enabled = false;
    }

}
