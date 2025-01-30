using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    public class ExplodeableHead : MonoBehaviour, IHittable, IResettable
    {
        [SerializeField] private bool manualExplosion;
        [SerializeField] private Animator animator;
        [SerializeField] private string explodeAnimation;
        [SerializeField] private string idleAnimation;
        [SerializeField] private GameObject[] headExplosionObjects;
        [SerializeField] private GameObject[] defaultObjects;

        private Action onExplodedEvent;
        private bool isExploded = false;

        public void ResetComponent()
        {
            foreach (var h in headExplosionObjects)
            {
                h.SetActive(false);
            }
            foreach (var d in defaultObjects)
            {
                d.SetActive(true);
            }
            animator.Play(idleAnimation);

            isExploded = false;
        }

        public void TakeDamage(DamageData[] damage)
        {
            if (manualExplosion)
                return;

            Explode();
        }

        public void Explode()
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

            animator.Play(explodeAnimation);

            isExploded = true;
        }
    }
}