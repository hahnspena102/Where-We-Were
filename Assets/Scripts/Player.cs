using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField]private Rigidbody rb;
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference clickAction;
    [SerializeField] private float speed = 5f;

    [SerializeField] private Camera cam;
    [SerializeField] private Transform hoverProjector;
    [SerializeField] private float hoverOffset = 0.01f;
    private Vector2 moveInput;
    private float holdTime;
    private GameManager gameManager;
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


    }
    void FixedUpdate()
    {
        if (gameManager.CurrentState == GameState.Answering)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }
        rb.linearVelocity = new Vector3(moveInput.x, rb.linearVelocity.y, moveInput.y) * speed;
    }
    
}
