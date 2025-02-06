using System.Collections.Generic;
using UnityEngine;

public class ZombieRandom : MonoBehaviour
{
    public Animator animator;
    public AnimationClip[] idleAnimations;
    public AnimationClip[] walkForwardAnimations;
    private AnimatorOverrideController overrideController;
    
    void Start()
    {
        // Get the existing Animator Controller
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);

        // Assign the new AnimatorOverrideController to the Animator
        animator.runtimeAnimatorController = overrideController;

        SetRandomAnimation();
    }

    public void SetRandomAnimation()
    {
        // Pick a random idle animation from the array
        AnimationClip selectedIdle = idleAnimations[Random.Range(0, idleAnimations.Length)];
        AnimationClip selectedWalk = walkForwardAnimations[Random.Range(0, walkForwardAnimations.Length)];

        // Override the idle animation in the Blend Tree
        overrideController["Idle_Default"] = selectedIdle;
        overrideController["Walk_Forward"] = selectedWalk;
    }
}
