using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Showcase helper behaviour to explode head of demo doll
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HeadExplosionDoll : MonoBehaviour, IHittable, ISimpleObserverable, IResettable
    {
        [SerializeField] private string headExplosionTrigger = "Explosion";
        [SerializeField] private GameObject[] headExplosionObjects;
        [SerializeField] private GameObject[] defaultObjects;

        [SerializeField] private Animator headAnimator;
        [SerializeField] private Animator bodyAnimator;
        [SerializeField] private string bodyAnimation;

        private Action onExplodedEvent;
        private bool isExploded = false;

        /// <summary>
        /// Take damage (cause head explosion)
        /// </summary>
        /// <param name="damage"></param>
        public void TakeDamage(DamageData[] damage)
        {
            if (isExploded)
                return;

            foreach (var h in headExplosionObjects)
            {
                h.SetActive(true);
            }
            foreach (var d in defaultObjects)
            {
                d.SetActive(false);
            }
            headAnimator.SetTrigger(headExplosionTrigger);
            if (onExplodedEvent != null)
            {
                onExplodedEvent();
            }
            bodyAnimator.Play(bodyAnimation);

            isExploded = true;
        }

        private void Start()
        {
            ResetComponent();
        }

        [ContextMenu("Reset Component")]
        public void ResetComponent()
        {
            isExploded = false;
            headAnimator.ResetTrigger(headExplosionTrigger);
            headAnimator.Play("Idle");
            bodyAnimator.Play("Idle");
            foreach (var h in headExplosionObjects)
            {
                h.SetActive(false);
            }
            foreach (var d in defaultObjects)
            {
                d.SetActive(true);
            }
        }

        public void AddListener(Action callback)
        {
            onExplodedEvent += callback;
        }

        public void RemoveListener(Action callback)
        {
            onExplodedEvent -= callback;
        }
    }
}