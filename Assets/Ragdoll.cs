using System;
using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    private Rigidbody[] rigidbodies;
    private Animator animator;

    private void Start()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();
        DisableRagdoll();
    }
    
    public void EnableRagdoll()
    {
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = false;
        }
        animator.enabled = false;
    }
    
    public void DisableRagdoll()
    {
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = true;
        }
        animator.enabled = true;
    }
}
