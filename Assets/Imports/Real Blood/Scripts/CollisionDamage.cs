using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Provides damage to IHittable objects on Unity Collision and Trigger events
    /// </summary>
    public class CollisionDamage : MonoBehaviour
    {
        [SerializeField] private bool continious = false;
        [SerializeField] private float damageInterval = 0.1f;
        [SerializeField] private string[] ignoreTags;

        private float lastDamageTime = 0;

        private void OnCollisionEnter(Collision collision)
        {
            if (!continious)
                OnHitted(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (continious)
                OnHitted(collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!continious)
                OnHitted(other, transform.position, -transform.forward);
        }

        private void OnTriggerStay(Collider other)
        {
            if (continious)
                OnHitted(other, transform.position, -transform.forward);
        }

        private void OnHitted(Collider collider, Vector3 point, Vector3 normal)
        {
            if (ignoreTags != null)
            {
                bool ignore = false;

                foreach (var t in ignoreTags)
                {
                    if (collider.CompareTag(t))
                    {
                        ignore = true;
                    }
                }

                if (ignore)
                    return;
            }

            var hittable = collider.GetComponent<IHittable>();

            if (hittable != null)
            {
                if (Time.time < lastDamageTime + damageInterval)
                    return;

                lastDamageTime = Time.time;

                var damage = new DamageData();
                damage.amount = 1;
                damage.direction = transform.forward;
                damage.point = point;
                damage.normal = normal;

                DamageData[] damages = new DamageData[1];

                damages[0] = damage;

                hittable.TakeDamage(damages);
            }
        }

        private void OnHitted(Collision collision)
        {
            Vector3 point = Vector3.zero;
            Vector3 normal = Vector3.zero;

            foreach (var c in collision.contacts)
            {
                point += c.point;
                normal += c.normal;
            }

            point /= collision.contactCount;
            normal /= collision.contactCount;

            OnHitted(collision.collider, point, normal);
        }
    }
}