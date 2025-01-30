using Knife.RealBlood.Decals;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Knife.RealBlood
{
    /// <summary>
    /// Showcase behaviour to play damage on demo doll
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HittableDoll : MonoBehaviour, IResettable, ISimpleObserverable
    {
        [SerializeField] private HitBox[] hitboxes;
        [SerializeField] private int minHitIndex = 0;
        [SerializeField] private int maxHitIndex = 2;
        [SerializeField] private GameObject bloodHitPrefab;
        [SerializeField] private GameObject bloodHitPrefab2;
        [SerializeField] private CharacterDamagePainter painter;

        private Animator animator;

        private bool isHitted = false;
        private Action onHittedEvent;

        /// <summary>
        /// Subscribe on Damage event
        /// </summary>
        /// <param name="callback"></param>
        public void AddListener(Action callback)
        {
            onHittedEvent += callback;
        }

        /// <summary>
        /// Unsubscribe from Damage event
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveListener(Action callback)
        {
            onHittedEvent -= callback;
        }

        /// <summary>
        /// Reset all damage
        /// </summary>
        public void ResetComponent()
        {
            isHitted = false;
            painter.CleanDamage();
        }

        /// <summary>
        /// Take damage (with HitBox argument)
        /// </summary>
        /// <param name="damage">All damage that will be applied</param>
        /// <param name="hitbox">Hitted hitbox</param>
        public void TakeDamage(DamageData[] damage, HitBox hitbox)
        {
            int randomHit = Random.Range(minHitIndex, maxHitIndex);
            animator.CrossFadeInFixedTime("Hit" + randomHit, 0.1f, 0, 0);

            Vector3 averagePoint = Vector3.zero;
            Vector3 averageNormal = Vector3.zero;
            foreach (var d in damage)
            {
                averagePoint += d.point;
                averageNormal += d.normal;
                painter.Paint(d.point, d.normal, 0);
            }

            averagePoint /= damage.Length;
            averageNormal /= damage.Length;

            var instance = Instantiate(bloodHitPrefab, averagePoint, Quaternion.LookRotation(averageNormal));
            Destroy(instance, 5f);

            if (bloodHitPrefab2 != null)
            {
                instance = Instantiate(bloodHitPrefab2, averagePoint, Quaternion.LookRotation(averageNormal));
                instance.transform.SetParent(hitbox.transform);
                Destroy(instance, 5f);
            }

            if (!isHitted)
            {
                if (onHittedEvent != null)
                    onHittedEvent();
            }

            isHitted = true;
        }

        private void Start()
        {
            animator = GetComponent<Animator>();

            foreach (var h in hitboxes)
            {
                var hitbox = h;
                h.AddListener(d => TakeDamage(d, hitbox));
            }
        }
    }
}