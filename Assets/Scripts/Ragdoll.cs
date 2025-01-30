using System;
using UnityEngine;
using UnityEngine.AI;

public class Ragdoll : MonoBehaviour
{
    private Rigidbody[] rigidbodies;
    private Animator animator;
    private AILocomotion locomotion;

    private void Start()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();
        locomotion = GetComponent<AILocomotion>();
        DisableRagdoll();
    }
    
    public void EnableRagdoll()
    {
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = false;
        }
        animator.enabled = false;
        locomotion.enabled = false;
    }
    
    public void DisableRagdoll()
    {
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = true;
        }
        animator.enabled = true;
        locomotion.enabled = true;
    }
}
