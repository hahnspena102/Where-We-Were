using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField]private Rigidbody rb;
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    [SerializeField] private float speed = 5f;
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
        

    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(-moveInput.x, rb.linearVelocity.y, -moveInput.y) * speed;
    }
    
}
