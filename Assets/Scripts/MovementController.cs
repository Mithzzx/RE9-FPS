using System;
using UnityEngine;
using UnityEngine.Serialization;

public class MovementController: MonoBehaviour
{
    
    [Header("Movement Settings")]
    [SerializeField] public float moveSpeed = 5f;
    [SerializeField] public float groundDrag = 6f;
    
    [Header("Jump Settings")]
    [SerializeField] public float jumpForce = 5f;
    [SerializeField] public float jumpCooldown = 0.25f;
    [SerializeField] public float airMultiplier = 0.5f;
    [SerializeField] public bool readyToJump = true;
    
    [Header("Ground Check")]
    [SerializeField] private bool grounded;
    
    public Transform orientation;
    
    Vector3 moveDirection;
    Rigidbody rb;
    
    private InputHandler input;
    

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

        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }
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
        moveDirection = orientation.forward * input.MoveInput.y + orientation.right * input.MoveInput.x;
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * airMultiplier * 10f, ForceMode.Force);
        }
    }
    
    [Obsolete("Obsolete")]
    private void SpeedControl()
    {
        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if (flatVelocity.magnitude > moveSpeed)
        {
            rb.linearVelocity = new Vector3(flatVelocity.normalized.x * moveSpeed, rb.linearVelocity.y, flatVelocity.normalized.z * moveSpeed);
        }
    }
    
    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    
    private void ResetJump()
    {
        readyToJump = true;
    }
}
