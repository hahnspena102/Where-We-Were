using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference clickAction;
    [SerializeField] private float speed = 2f;

    [SerializeField] private Camera cam;
    [SerializeField] private Transform hoverProjector;

    [SerializeField] private Entry currentHoverEntry;
    [SerializeField] private PlayerData playerData;
    private Vector2 moveInput;
    private float holdTime;
    private float holdDuration = 1f;
    private GameManager gameManager;
    private Animator animator;
    private float facingDirection = 1f;

    public Entry CurrentHoverEntry { get => currentHoverEntry; set => currentHoverEntry = value; }

    void Start()
    {   DisableIntersectionColliders();
        rb = GetComponent<Rigidbody>();
        gameManager = FindAnyObjectByType<GameManager>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();

        if (clickAction.action.IsPressed() && gameManager.HoverPosition != Vector3.zero && gameManager.CurrentState == GameplayState.Prompting) 
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
            if (drawingDisplay != null)
            {
                currentHoverEntry = drawingDisplay.Entry;
            }
        }

        if (gameManager.CurrentState != GameplayState.Answering && gameManager.CurrentState != GameplayState.Drawing)
        {
            animator.SetFloat("speed", moveInput.magnitude);
            // rotate player facing left or right based on input
            if (moveInput.x > 0.1f || moveInput.x < -0.1f) {
                facingDirection = Mathf.Sign(moveInput.x);
            }
       
        }
        

        Quaternion targetRotation = Quaternion.Euler(0f, facingDirection < 0f ? 0f : 180f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    void FixedUpdate()
{
    if (gameManager.CurrentState == GameplayState.Answering)
    {
        if (gameManager.CurrentState == GameplayState.Answering || gameManager.CurrentState == GameplayState.Drawing)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }
            Vector3 groundedMovement = new Vector3(moveInput.x, 0f, moveInput.y) * speed;
            if (OnSlope(out RaycastHit groundHit)) {
                    groundedMovement = Vector3.ProjectOnPlane(groundedMovement, groundHit.normal);
                    rb.linearVelocity = groundedMovement;
        } else {
                    groundedMovement.y = rb.linearVelocity.y;
                    rb.linearVelocity = groundedMovement;
        }
    }

    rb.AddForce(Vector3.down * 20f, ForceMode.Acceleration);

        Vector3 slopeMovement = new Vector3(moveInput.x, 0f, moveInput.y) * speed;

        if (OnSlope(out RaycastHit slopeHit))
    {
            Vector3 slopeMove = Vector3.ProjectOnPlane(slopeMovement, slopeHit.normal);
        slopeMove.y = Mathf.Min(slopeMove.y, 0f);
        rb.linearVelocity = new Vector3(slopeMove.x, rb.linearVelocity.y + slopeMove.y, slopeMove.z);
    }
    else
    {
            rb.linearVelocity = new Vector3(slopeMovement.x, rb.linearVelocity.y, slopeMovement.z);
    }
}

    bool OnSlope(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out hit, 1.2f))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            // Must be angled but not a wall, AND hit point must be below our feet
            return angle > 2f && angle < 45f && hit.point.y < transform.position.y + 0.05f;
        }
        return false;
    }

    public float GetHoldPercentage()
    {
        return Mathf.Clamp01(holdTime / holdDuration);
    }

    public void SelectPrompt1()
    {
        SelectPrompt(0);
    }

    public void SelectPrompt2()
    {
        SelectPrompt(1);
    }

    public void SelectPrompt3()
    {
        SelectPrompt(2);
    }

    private void SelectPrompt(int promptIndex)
    {
        if (playerData == null || gameManager == null)
        {
            return;
        }

        playerData.PromptIndex = promptIndex;
        gameManager.RestartGame();
    }
    void DisableIntersectionColliders() 
    {
        // Road Architect intersection objects are tagged/named with "intersection"
        // This finds every mesh collider in the scene on objects with that name
        MeshCollider[] allMeshColliders = FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
        
        foreach (MeshCollider col in allMeshColliders)
        {
            if (col.gameObject.name.ToLower().Contains("inter"))
            {
                col.enabled = false;
            }
        }

    }
}