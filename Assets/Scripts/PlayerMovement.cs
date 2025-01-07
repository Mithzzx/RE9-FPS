using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float groundDrag = 6f;
    
    [Header("Jumping")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private float airMultiplier = 0.5f;
    [SerializeField] private bool readyToJump;

    [Header("Slope Handling")] 
    [SerializeField] private bool onSlope;
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private bool exitingSlope;
    private RaycastHit slopeHit;
    
    [Header("Ground Check")]
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private bool isGrounded;
    
    [Header("References")]
    [SerializeField] private InputHandler input;
    [SerializeField] private Transform orientation;

    private Vector3 moveDirection;
    
    private Rigidbody rb;
    
    public MovementState state;
     
    public enum MovementState
    {
        Walking,
        Sprinting,
        Air
    }
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }
    
    private void Update()
    {  
        speed = rb.linearVelocity.magnitude;
        
        // Check if player is grounded
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight / 2 + 0.2f, groundMask);
        
        // Apply drag
        rb.linearDamping = isGrounded ? groundDrag : 0;
        
        // Speed control
        SpeedControl();
        
        // State handling
        StateHandler();
        
        // Jump
        if (input.JumpTriggered && readyToJump && isGrounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        onSlope = OnSlope();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }
    
    private void StateHandler()
    {
        // Mode - Sprinting
        if (input.SprintTriggered && isGrounded)
        {
            state = MovementState.Sprinting;
            moveSpeed = sprintSpeed;
        }
        
        // Mode - Walking
        else if (isGrounded)
        {
            state = MovementState.Walking;
            moveSpeed = walkSpeed;
        }
        
        // Mode - Air
        else
        {
            state = MovementState.Air;
        }
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * input.MoveInput.y + orientation.right * input.MoveInput.x;
        moveDirection.y = 0;
        moveDirection.Normalize();

        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * (moveSpeed * 20f), ForceMode.Force);
            if (rb.linearVelocity.y > 0f)
            {
                rb.AddForce(Vector3.down * 50f ,ForceMode.Force);
            }
        }
        if (isGrounded)
        {
            rb.AddForce(moveDirection * (moveSpeed * 10f), ForceMode.Force);
        }
        else if (!isGrounded)
        {
            rb.AddForce(moveDirection * (moveSpeed * 10f * airMultiplier), ForceMode.Force);
        }

        rb.useGravity = !OnSlope();
    }
    
    private void SpeedControl()
    {
        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > moveSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
            if (flatVelocity.magnitude > moveSpeed)
            {
                Vector3 limitedVelocity = flatVelocity.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
            }
        }
    }
    
    private void Jump()
    {
        exitingSlope = true;
        // Reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    
    private void ResetJump()
    {
        exitingSlope = false;
        readyToJump = true;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}
