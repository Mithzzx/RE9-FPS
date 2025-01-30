using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Behaviour that apply random speed to Animator
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class RandomAnimatorSpeed : MonoBehaviour
    {
        [SerializeField] private float minSpeed = 0.8f;
        [SerializeField] private float maxSpeed = 1.2f;

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            animator.speed = Random.Range(minSpeed, maxSpeed);
        }
    }
}