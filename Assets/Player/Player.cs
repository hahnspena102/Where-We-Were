using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField]private Rigidbody rb;
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    [SerializeField] private float speed = 5f;

    [SerializeField] private Camera cam;
    [SerializeField] private Transform hoverProjector;
    [SerializeField] private float hoverOffset = 0.01f;
    private Vector2 moveInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
   void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();

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
                hoverProjector.gameObject.SetActive(true);

                hoverProjector.position = hit.point + hit.normal * hoverOffset;
                hoverProjector.rotation = Quaternion.LookRotation(hit.normal);
            }
            else
            {
                hoverProjector.gameObject.SetActive(false);
            }
        }
        else
        {
            hoverProjector.gameObject.SetActive(false);
        }
    }
    void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(moveInput.x, rb.linearVelocity.y, moveInput.y) * speed;
    }
    
}
