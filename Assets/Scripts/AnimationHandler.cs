using System;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputHandler input;
    [SerializeField] private PlayerMovement pm;
    [SerializeField] private Animator lowerBodyAnim;
    
    [Header("Settings")]
    [SerializeField] private float xVelocity;
    [SerializeField] private float yVelocity;
    [SerializeField] private float acceleration;
    public float increaseSpeed = 2f; // Speed of the increase
    public float decreaseSpeed = 1f; // Target value for the x-axis
    private float targetValue;
    
    private bool idle = true;
    private bool jumped;
    private bool inAir;
    
    [Header("Hashes")]
    private readonly int xVelocityHash = Animator.StringToHash("xVelocity");
    private readonly int yVelocityHash = Animator.StringToHash("yVelocity");
    private readonly int idleHash = Animator.StringToHash("idel");
    private readonly int jumpedHash = Animator.StringToHash("jumped");
    private readonly int inAirHash = Animator.StringToHash("inAir");

    void Update()
    {
        // Get Vector2's x and y values (e.g., from Input.GetAxis)
        Vector2 inputVector = input.MoveInput; // -1 to 1 range for vertical input

        // Determine targetValueY
        if (input.SprintTriggered) // Shift key pressed
        {
            targetValue = 2f;
        }
        else if (inputVector.magnitude == 0) // No input for y
        {
            targetValue = 0f;
        }
        else
        {
            targetValue = 0.5f;
        }
        

        // Smoothly increase or decrease valueY based on targetValueY
        if (yVelocity < targetValue)
        {
            yVelocity = Mathf.Lerp(yVelocity, targetValue*inputVector.y, Time.deltaTime * increaseSpeed);
        }
        else if (yVelocity > targetValue)
        {
            yVelocity = Mathf.Lerp(yVelocity, targetValue*inputVector.y, Time.deltaTime * decreaseSpeed);
        }

        // Smoothly increase or decrease valueX based on targetValueX
        if (xVelocity < targetValue)
        {
            xVelocity = Mathf.Lerp(xVelocity, targetValue*inputVector.x, Time.deltaTime * increaseSpeed);
        }
        else if (xVelocity > targetValue)
        {
            xVelocity = Mathf.Lerp(xVelocity, targetValue*inputVector.x, Time.deltaTime * decreaseSpeed);
        }

        // Set the xVelocity and yVelocity parameters in the Animator
        lowerBodyAnim.SetFloat(xVelocityHash, xVelocity);
        lowerBodyAnim.SetFloat(yVelocityHash, yVelocity);
        
        // Set the idle parameter in the Animator
        idle = pm.rb.linearVelocity.magnitude < 0.1f;
        lowerBodyAnim.SetBool(idleHash,idle);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            lowerBodyAnim.Play("Jumping Up");
        }

        inAir = !(pm.isGrounded);
        lowerBodyAnim.SetBool(inAirHash,inAir);
    }
}
