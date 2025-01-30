using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Simple projectile
    /// </summary>
    public class ExampleDamageable : MonoBehaviour
    {
        [SerializeField] private float speed = 3f;
        [SerializeField] private LayerMask layerMask;

        private Vector3 velocity;

        private void Start()
        {
        }

        private void OnEnable()
        {
            velocity = transform.forward * speed;
        }

        private void FixedUpdate()
        {
            Vector3 deltaPosition = velocity * Time.fixedDeltaTime;

            Ray r = new Ray(transform.position, deltaPosition);
            RaycastHit hitInfo;

            if (Physics.Raycast(r, out hitInfo, deltaPosition.magnitude, layerMask, QueryTriggerInteraction.Ignore))
            {
                OnHitted(hitInfo);
            }

            transform.position += deltaPosition;
        }

        private void OnHitted(RaycastHit hitInfo)
        {
            var hittable = hitInfo.collider.GetComponent<IHittable>();

            if (hittable != null)
            {
                var damage = new DamageData();
                damage.amount = 1;
                damage.direction = transform.forward;
                damage.point = hitInfo.point;
                damage.normal = -hitInfo.normal;

                DamageData[] damages = new DamageData[1];

                damages[0] = damage;

                hittable.TakeDamage(damages);
            }
            Destroy(gameObject);
        }
    }
}