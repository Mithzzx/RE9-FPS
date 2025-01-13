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
    private bool idle = true;
    
    [Header("Hashes")]
    private readonly int xVelocityHash = Animator.StringToHash("xVelocity");
    private readonly int yVelocityHash = Animator.StringToHash("yVelocity");
    private readonly int idleHash = Animator.StringToHash("idel");

    private void Update()
    {
        bool sprinting = input.SprintTriggered;
        
        float xInput = input.MoveInput.x;
        float yInput = input.MoveInput.y;
        
        idle = !(pm.rb.linearVelocity.magnitude > 0.1f);
        
        xVelocity = Mathf.Lerp(xVelocity, xInput * pm.moveSpeed, acceleration * Time.deltaTime);
        yVelocity = Mathf.Lerp(yVelocity, yInput * pm.moveSpeed, acceleration * Time.deltaTime);
        
        lowerBodyAnim.SetFloat(xVelocityHash, xVelocity);
        lowerBodyAnim.SetFloat(yVelocityHash, yVelocity);
        lowerBodyAnim.SetBool(idleHash,idle);
    }
}
