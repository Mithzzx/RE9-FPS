using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Example behaviour to shoot off hand
    /// </summary>
    public class DemoDollHand : MonoBehaviour, IHittable, IResettable, ISimpleObserverable
    {
        [SerializeField] private SkinnedMeshRenderer doll;
        [SerializeField] private int blendShapeIndex = 0;
        [SerializeField] private GameObject[] disableObjects;
        [SerializeField] private GameObject[] enableObjects;
        [SerializeField] private Animator animator;
        [SerializeField] private string animationName;

        private Action onExplodedEvent;

        private void Start()
        {
            ResetComponent();
        }

        public void ResetComponent()
        {
            CancelExplode();
        }

        private void CancelExplode()
        {
            doll.SetBlendShapeWeight(blendShapeIndex, 0);

            foreach (var g in disableObjects)
            {
                g.SetActive(true);
            }

            foreach (var g in enableObjects)
            {
                g.SetActive(false);
            }
        }

        public void TakeDamage(DamageData[] damage)
        {
            ExplodeHand();
        }

        private void ExplodeHand()
        {
            if (onExplodedEvent != null)
                onExplodedEvent();

            doll.SetBlendShapeWeight(blendShapeIndex, 100);
            animator.Play(animationName, 0, 0);

            foreach (var g in disableObjects)
            {
                g.SetActive(false);
            }

            // There we enable objects (also FXes)
            foreach (var g in enableObjects)
            {
                g.SetActive(true);
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