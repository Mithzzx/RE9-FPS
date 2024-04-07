using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField]Animator camanimator;
    int crouch = Animator.StringToHash("crouch");
    int ismoving = Animator.StringToHash("moving");

    Animator animator;
    int xmove = Animator.StringToHash("xVelocity");
    int ymove = Animator.StringToHash("yVelocity");
    int crouched = Animator.StringToHash("isCrouch");

    [SerializeField] float acc;
    [SerializeField] float dec;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void ProcessAnimation(Vector2 v, bool isSprinting, bool isCrouched)
    {
        Lurp(v.y , xmove, isSprinting,isCrouched);
        Lurp(v.x, ymove, isSprinting,isCrouched);

        if (isCrouched) ToggleCrouch(true,v);
        else ToggleCrouch(false,v);
    }

    private void Lurp(float v ,int hash,bool isSprinting, bool isCrouched)
    {
        float hashLimit;

        if (isSprinting || isCrouched) hashLimit = 1f;
        else hashLimit = 0.5f;

        if (v > 0)
        {
            if (animator.GetFloat(hash) < hashLimit)
            {
                animator.SetFloat(hash, animator.GetFloat(hash) + acc * Time.deltaTime);
            }
            else animator.SetFloat(hash, animator.GetFloat(hash) - dec * Time.deltaTime);
        }
        else
        {
            if (animator.GetFloat(hash) > 0f)
            {
                animator.SetFloat(hash, animator.GetFloat(hash) - dec * Time.deltaTime);
            }
        }

        if (v < 0)
        {
            if (animator.GetFloat(hash) > -hashLimit)
            {
                animator.SetFloat(hash, animator.GetFloat(hash) - acc * Time.deltaTime);
            }
            else animator.SetFloat(hash, animator.GetFloat(hash) + dec * Time.deltaTime);
        }
        else
        {
            if (animator.GetFloat(hash) < 0f)
            {
                animator.SetFloat(hash, animator.GetFloat(hash) + dec * Time.deltaTime);
            }
        }
    }
    private void ToggleCrouch(bool state,Vector2 vector)
    {
        animator.SetBool(crouched, state);
        camanimator.SetBool(crouch, state);
        camanimator.SetBool(ismoving, vector.magnitude > 0);
    }

}