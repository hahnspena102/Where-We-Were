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
    private float holdDuration = 1f;
    private GameManager gameManager;
    private Animator animator;

    public Entry CurrentHoverEntry { get => currentHoverEntry; set => currentHoverEntry = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gameManager = FindFirstObjectByType<GameManager>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
   void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();

        if (clickAction.action.IsPressed() && gameManager.HoverPosition != Vector3.zero && gameManager.CurrentState == GameState.Prompting) 
        {
            holdTime += Time.deltaTime;

            if (holdTime >= holdDuration)
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

        animator.SetFloat("speed", moveInput.magnitude);
        // rotate player facing left or right based on input
        if (moveInput.x > 0.1f) {
            //Debug.Log("Facing right");
        } else if (moveInput.x < -0.1f)
        {
            //Debug.Log("Facing left");
        }
            
        if (moveInput.x > 0.1f || moveInput.x < -0.1f) {
            
            Vector3 lookDirection = new Vector3(0, 0, -moveInput.x);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 10f);
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

    public float GetHoldPercentage()
    {
        return Mathf.Clamp01(holdTime / holdDuration);
    }
}
