using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood.SimpleController
{
    /// <summary>
    /// FPSDamageFX behaviour to play random animation on damage
    /// </summary>
    public class AnimatorFX : FPSDamageFX
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string[] animationNames;
        [SerializeField] private float minimumIntervalBeforeNext = 0.3f;

        private float lastSpawnTime;

        public override void PlayFX(DamageData[] damage)
        {
            if (Time.time < lastSpawnTime + minimumIntervalBeforeNext)
                return;

            animator.Play(animationNames[Random.Range(0, animationNames.Length)], 0, 0);
        }
    }
}