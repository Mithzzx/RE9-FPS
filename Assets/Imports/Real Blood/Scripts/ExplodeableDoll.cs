using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Showcase helper behaviour to explode demo doll
    /// </summary>
    public class ExplodeableDoll : MonoBehaviour, IHittable, IResettable, ISimpleObserverable
    {
        [SerializeField] private string explosionTrigger = "Explode";
        [SerializeField] private GameObject[] explosionObjects;
        [SerializeField] private GameObject[] disableExplosionObjects;

        private Action onExplodedEvent;

        private Animator animator;
        private Collider attachedCollider;

        private bool isExploded = false;

        private void Start()
        {
            animator = GetComponent<Animator>();
            attachedCollider = GetComponent<Collider>();
            ResetComponent();
        }

        /// <summary>
        /// Take damage (cause explosion)
        /// </summary>
        /// <param name="damage"></param>
        public void TakeDamage(DamageData[] damage)
        {
            if (isExploded)
                return;

            attachedCollider.enabled = false;
            animator.SetTrigger(explosionTrigger);
            foreach (var g in explosionObjects)
            {
                g.SetActive(true);
            }
            foreach (var g in disableExplosionObjects)
            {
                g.SetActive(false);
            }
            if (onExplodedEvent != null)
            {
                onExplodedEvent();
            }

            isExploded = true;
        }

        /// <summary>
        /// Reset explosion
        /// </summary>
        [ContextMenu("Reset Component")]
        public void ResetComponent()
        {
            attachedCollider.enabled = true;
            isExploded = false;
            animator.ResetTrigger(explosionTrigger);
            animator.Play("Idle");
            foreach (var g in explosionObjects)
            {
                g.SetActive(false);
            }
            foreach (var g in disableExplosionObjects)
            {
                g.SetActive(true);
            }
        }

        /// <summary>
        /// Subscribe on explode event
        /// </summary>
        /// <param name="callback"></param>
        public void AddListener(Action callback)
        {
            onExplodedEvent += callback;
        }

        /// <summary>
        /// Unsubscribe from explode event
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveListener(Action callback)
        {
            onExplodedEvent -= callback;
        }
    }
}