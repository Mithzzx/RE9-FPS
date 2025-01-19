using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    
    [Header("Movement")]
    [SerializeField] public float moveSpeed;
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float swingSpeed = 20f;
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
    [SerializeField] public bool isGrounded;
    
    [Header("References")]
    [SerializeField] private InputHandler input;
    [SerializeField] private Transform orientation;

    private Vector3 moveDirection;
    
    public Rigidbody rb;
    
    public MovementState state;
     
    public enum MovementState
    {
        Freeze,
        Walking,
        Sprinting,
        Swinging,
        Air
    }

    public bool freeze;
    
    public bool activeGrapple;
    public bool swinging;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }
    
    private void Update()
    {  
        speed = rb.linearVelocity.magnitude;
        
        // Check if player is grounded
        isGrounded = Physics.Raycast(orientation.position, Vector3.down, playerHeight / 2 + 0.2f, groundMask);
        
        // Apply drag
        rb.linearDamping = isGrounded && !activeGrapple ? groundDrag : 0;
        
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
        // Mode - Freeze
        if (freeze)
        {
            state = MovementState.Freeze;
            moveSpeed = 0;
            rb.linearVelocity = Vector3.zero;
        }
        // Mode - Sprinting
        else if (input.SprintTriggered && isGrounded)
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
        
        // Mode - Swinging
        else if (swinging)
        {
            state = MovementState.Swinging;
            moveSpeed = swingSpeed;
        }
        
        // Mode - Air
        else
        {
            state = MovementState.Air;
        }
    }

    private void MovePlayer()
    {
        if (activeGrapple) return;
        if (swinging) return;
        
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
        else if (isGrounded)
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
        if (activeGrapple) return;
        
        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > moveSpeed)
            {rb.linearVelocity = rb.linearVelocity.normalized * (moveSpeed * 0.8f);}
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
    
    private bool enableMovementOnNextTouch;
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;
        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f);
        
        Invoke(nameof(ResetRestrictions), 3f);
    }
    
    private Vector3 velocityToSet;
    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.linearVelocity = velocityToSet;
    }
    
    public void ResetRestrictions()
    {
        activeGrapple = false;  
    }

    private void OnCollisionEnter(Collision other)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();
            
            GetComponent<Grappling>().StopGrapple();
        }
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
    
    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) 
                                               + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }
}
