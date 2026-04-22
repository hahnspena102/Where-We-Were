using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField]private Rigidbody rb;
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference clickAction;
    [SerializeField] private float speed = 2f;

    [SerializeField] private Camera cam;
    [SerializeField] private Transform hoverProjector;
    [SerializeField] private float hoverOffset = 0.01f;
    [SerializeField] private Entry currentHoverEntry;
    private Vector2 moveInput;
    private float holdTime;
    private GameManager gameManager;

    public Entry CurrentHoverEntry { get => currentHoverEntry; set => currentHoverEntry = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    // Update is called once per frame
   void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();

        if (clickAction.action.IsPressed() && gameManager.HoverPosition != Vector3.zero)
        {
            holdTime += Time.deltaTime;

            if (holdTime >= 1f)
            {
                gameManager.PlayerHold();
            }
        }
        else
        {
            holdTime = 0f;
        }

        currentHoverEntry = null;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {            
            DrawingDisplay drawingDisplay = hit.collider.GetComponent<DrawingDisplay>();
            if (drawingDisplay != null)            {
                currentHoverEntry = drawingDisplay.Entry;
            }
          
        }


    }
    void FixedUpdate()
    {
        if (gameManager.CurrentState == GameState.Answering)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }
        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y) * speed;
       if (OnSlope(out RaycastHit hit)) {
            movement = Vector3.ProjectOnPlane(movement, hit.normal);
            rb.linearVelocity = movement;
       } else {
            movement.y = rb.linearVelocity.y;
            rb.linearVelocity = movement;
       }
    }

    bool OnSlope(out RaycastHit hit) {
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f)) {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            return angle > 0f && angle < 45f; 
        }
        return false;
    }
}
