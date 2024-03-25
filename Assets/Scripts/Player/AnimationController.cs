using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    Animator animator;
    int xmove = Animator.StringToHash("xVelocity");
    int ymove = Animator.StringToHash("yVelocity");

    [SerializeField] float acc;
    [SerializeField] float dec;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void ProcessAnimation(Vector2 v, bool isSprinting)
    {
        Lurp(v.y , xmove,isSprinting);
        Lurp(v.x, ymove,isSprinting);

    }

    private void Lurp(float v ,int hash,bool isSprinting)
    {
        float hashLimit = 0.5f;
        if (isSprinting) hashLimit = 1f;
        if (v > 0)
        {
            if (animator.GetFloat(hash) < hashLimit)
            {
                animator.SetFloat(hash, animator.GetFloat(hash) + acc * Time.deltaTime);
            }
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
        }
        else
        {
            if (animator.GetFloat(hash) < 0f)
            {
                animator.SetFloat(hash, animator.GetFloat(hash) + dec * Time.deltaTime);
            }
        }
    }
}