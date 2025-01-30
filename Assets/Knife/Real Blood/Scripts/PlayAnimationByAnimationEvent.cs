using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Helper behaviour that can play animation on other Animator by AnimationEvent
    /// </summary>
    public class PlayAnimationByAnimationEvent : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string animationName;

        private void PlayAnimationEvent()
        {
            animator.Play(animationName, 0, 0);
        }
    }
}