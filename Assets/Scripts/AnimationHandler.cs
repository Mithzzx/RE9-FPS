using System;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputHandler input;
    [SerializeField] private PlayerMovement pm;
    [SerializeField] private GameObject lowerBody;
    [SerializeField] private GameObject shadowBody;
    [SerializeField] private Transform orientation;
    
    
    [Header("Settings")]
    [SerializeField] private float xVelocity;
    [SerializeField] private float yVelocity;
    [SerializeField] private float acceleration;
    public float increaseSpeed = 2f; // Speed of the increase
    public float decreaseSpeed = 1f; // Target value for the x-axis
    private float targetValue;
    [SerializeField] private float angleDifference;
    
    private bool idle = true;
    private bool jumped;
    private bool inAir;
    
    [Header("Hashes")]
    private readonly int xVelocityHash = Animator.StringToHash("xVelocity");
    private readonly int yVelocityHash = Animator.StringToHash("yVelocity");
    private readonly int idleHash = Animator.StringToHash("idel");
    private readonly int jumpedHash = Animator.StringToHash("jumped");
    private readonly int inAirHash = Animator.StringToHash("inAir");
    private readonly int turnHash = Animator.StringToHash("turn");
    private readonly int turnRight90Hash = Animator.StringToHash("Right Turn 90");
    private readonly int turnLeft90Hash = Animator.StringToHash("Left Turn 90");
    private Animator lowerBodyAnimator;
    private Animator shadowBodyAnimator;
    private lower lowerBodyScript;
    private Transform lowerBodyTransform;
    private Transform shadowBodyTransform;

    private void Start()
    {
        lowerBodyAnimator = lowerBody.GetComponent<Animator>();
        lowerBodyScript = lowerBody.GetComponent<lower>();
        lowerBodyTransform = lowerBody.transform;
        
        shadowBodyAnimator = shadowBody.GetComponent<Animator>();
        shadowBodyTransform = shadowBody.transform;
    }

    void Update()
    {
        // Determine targetValueY
        if (input.SprintTriggered) // Shift key pressed
        {
            targetValue = 2f;
        }
        else if (input.MoveInput.magnitude == 0) // No input for y
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
            yVelocity = Mathf.Lerp(yVelocity, targetValue*input.MoveInput.y, Time.deltaTime * increaseSpeed);
        }
        else if (yVelocity > targetValue)
        {
            yVelocity = Mathf.Lerp(yVelocity, targetValue*input.MoveInput.y, Time.deltaTime * decreaseSpeed);
        }

        // Smoothly increase or decrease valueX based on targetValueX
        if (xVelocity < targetValue)
        {
            xVelocity = Mathf.Lerp(xVelocity, targetValue*input.MoveInput.x, Time.deltaTime * increaseSpeed);
        }
        else if (xVelocity > targetValue)
        {
            xVelocity = Mathf.Lerp(xVelocity, targetValue*input.MoveInput.x, Time.deltaTime * decreaseSpeed);
        }

        if (!lowerBodyScript.rootMotion)
        {
            lowerBodyAnimator.applyRootMotion = false;
            shadowBodyAnimator.applyRootMotion = false;
            lowerBodyScript.rootMotion = true;
        }
        
        // Calculate the difference in the Y-axis (yaw)
        float yawDifference = Mathf.DeltaAngle(lowerBodyTransform.eulerAngles.y, orientation.eulerAngles.y);
        
        // Turn the player to the direction of the camera
        if (pm.rb.linearVelocity.magnitude > 0.1f)
        {
            lowerBodyAnimator.SetBool(turnHash, true);
            lowerBodyAnimator.applyRootMotion = false;
            lowerBodyTransform.rotation = Quaternion.Euler(0, orientation.eulerAngles.y, 0);
            shadowBodyTransform.rotation = Quaternion.Euler(0, orientation.eulerAngles.y, 0);
        }
        // Check if the difference is approximately 80 degrees
        else if (Mathf.Abs(yawDifference) >= 75)
        {
            lowerBodyAnimator.applyRootMotion = true;
            shadowBodyAnimator.applyRootMotion = true;
            
            if (yawDifference > 0)
            {
                // Turned 80 degrees to the right
                lowerBodyAnimator.Play(turnRight90Hash);
                shadowBodyAnimator.Play(turnRight90Hash);
            }
            else
            {
                // Turned 80 degrees to the left
                lowerBodyAnimator.Play(turnLeft90Hash);
                shadowBodyAnimator.Play(turnLeft90Hash);
            }
            
            if (Mathf.Abs(yawDifference) >= 120)
            {
                lowerBodyTransform.rotation= Quaternion.Euler(0, orientation.eulerAngles.y, 0);
                shadowBodyTransform.rotation= Quaternion.Euler(0, orientation.eulerAngles.y, 0);
            }
        }

        // Set the xVelocity and yVelocity parameters in the Animator
        lowerBodyAnimator.SetFloat(xVelocityHash, xVelocity);
        lowerBodyAnimator.SetFloat(yVelocityHash, yVelocity);
        
        // Set the xVelocity and yVelocity parameters in the Animator
        shadowBodyAnimator.SetFloat(xVelocityHash, xVelocity);
        shadowBodyAnimator.SetFloat(yVelocityHash, yVelocity);
        
        // Set the idle parameter in the Animator
        idle = pm.rb.linearVelocity.magnitude < 0.1f;
        lowerBodyAnimator.SetBool(idleHash,idle);
        shadowBodyAnimator.SetBool(idleHash,idle);
        

        if (Input.GetKeyDown(KeyCode.Space))
        {
            lowerBodyAnimator.Play("Jumping Up");
            shadowBodyAnimator.Play("Jumping Up");
        }

        inAir = !(pm.isGrounded);
        lowerBodyAnimator.SetBool(inAirHash,inAir);
        shadowBodyAnimator.SetBool(inAirHash,inAir);
    }
}
