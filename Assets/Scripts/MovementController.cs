using System;
using UnityEngine;

public class MovementController: MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    
    [Header("Movement Settings")]
    [SerializeField] public float walkSpeed = 6f;
    [SerializeField] public float sprintSpeed = 9f;
    private float moveSpeed ;
    [SerializeField] public float groundDrag = 6f;
    
    [Header("Jump Settings")]
    [SerializeField] public float jumpForce = 5f;
    [SerializeField] public float jumpCooldown = 0.25f;
    [SerializeField] public float airMultiplier = 0.5f;
    [SerializeField] public bool readyToJump = true;
    private bool exitingSlope;
    
    [Header("Slope Settings")]
    [SerializeField] public float maxSlopAngle = 30f;
    private RaycastHit slopeHit;
    
    [Header("Ground Check")]
    [SerializeField] private bool grounded;
    
    public Transform orientation;
    
    Vector3 moveDirection;
    Rigidbody rb;
    
    private InputHandler input;

    public bool freeze;
    
    private bool activeGrapple;
    
    public MovementState movementState;
    public enum MovementState
    {
        Freeze,
        Walking,
        Sprinting,
        Air,
    }

    private void Awake()
    {
        input = GetComponent<InputHandler>();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }
    
    private void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);

        if (grounded && !activeGrapple)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }
        StateHandler();
    }
    private void FixedUpdate()
    {
        transform.rotation = orientation.rotation;
        MovePlayer();
        SpeedControl();
        
        if (input.JumpTriggered && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }
    
    private void MovePlayer()
    {
        if (activeGrapple) return;
        
        moveDirection = orientation.forward * input.MoveInput.y + orientation.right * input.MoveInput.x;
        
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * (moveSpeed * 10f), ForceMode.Force);
            
            if (rb.linearVelocity.y < 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            } 
        }
        
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * (moveSpeed * 10f), ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * (moveSpeed * airMultiplier * 10f), ForceMode.Force);
        }
        
        rb.useGravity = !OnSlope();
    }
    
    private void SpeedControl()
    {
        // Limit speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > moveSpeed)
            {
                rb.linearVelocity= rb.linearVelocity.normalized * moveSpeed;
            }
        }
        // Limit speed in general
        else
        {
            Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            if (flatVelocity.magnitude > moveSpeed)
            {
                rb.linearVelocity = new Vector3(flatVelocity.normalized.x * moveSpeed, rb.linearVelocity.y, flatVelocity.normalized.z * moveSpeed);
            }
        }
    }
    
    private void StateHandler()
    {
        //Mode - Freeze
        if (freeze)
        {
            movementState = MovementState.Freeze;
            moveSpeed = 0;
            rb.linearVelocity = Vector3.zero;
        }
        //Mode - Sprinting
        else if (input.SprintTriggered && grounded)
        {
            movementState = MovementState.Sprinting;
            moveSpeed = sprintSpeed;
        }
        //Mode - Walking
        else if (grounded)
        {
            movementState = MovementState.Walking;
            moveSpeed = walkSpeed;
        }
        //Mode - Air
        else
        {
            movementState = MovementState.Air;
        }
    }
    
    private void Jump()
    {
        exitingSlope = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    
    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
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

    private void OnCollisionEnter(Collision other)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();
        }
    }

    private void ResetRestrictions()
    {
        activeGrapple = false;
        GetComponent<Grappling>().StopGrapple();
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 1.1f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
    
    private Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) 
                                               + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));
        Debug.Log(velocityXZ);
        return velocityXZ*10f + velocityY;
    }
}
