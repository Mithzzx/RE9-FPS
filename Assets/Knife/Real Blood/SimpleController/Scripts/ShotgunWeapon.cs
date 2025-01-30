using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood.SimpleController
{
    /// <summary>
    /// Shotgun weapon
    /// </summary>
    public class ShotgunWeapon : Weapon
    {
        public int Bullets = 10;
        public float RandomAngle = 5f;

        Dictionary<IHittable, List<DamageData>> damages = new Dictionary<IHittable, List<DamageData>>();
        //Dictionary<IDamageable, List<Damage>> damages2 = new Dictionary<IDamageable, List<Damage>>();

        protected override void Shot()
        {

            handsAnimator.Play("Shot", 0, 0);

            damages.Clear();
            //damages2.Clear();
            for (int i = 0; i < Bullets; i++)
            {
                Vector3 direction = Camera.main.transform.forward;
                direction = Quaternion.AngleAxis(Random.Range(-RandomAngle, RandomAngle), Camera.main.transform.up) * direction;
                direction = Quaternion.AngleAxis(Random.Range(-RandomAngle, RandomAngle), Camera.main.transform.right) * direction;
                //Debug.DrawLine(Camera.main.transform.position, Camera.main.transform.position + direction * 155f, Color.red, 5f);

                Ray r = new Ray(Camera.main.transform.position, direction);
                RaycastHit hitInfo;

                if (Physics.Raycast(r, out hitInfo, 1000, ShotMask, QueryTriggerInteraction.Ignore))
                {

                    var hittable = hitInfo.collider.GetComponent<IHittable>();
                    if (hittable != null)
                    {
                        DamageData damage = new DamageData()
                        {
                            amount = Damage,
                            direction = r.direction,
                            normal = hitInfo.normal,
                            point = hitInfo.point
                        };

                        List<DamageData> damageDatasPerHittable;
                        if (!damages.TryGetValue(hittable, out damageDatasPerHittable))
                        {
                            damageDatasPerHittable = new List<DamageData>();
                            damages.Add(hittable, damageDatasPerHittable);
                        }
                        damageDatasPerHittable.Add(damage);
                    }

                }

                DebugShot(r, hitInfo);
            }

            foreach (var kv in damages)
            {
                kv.Key.TakeDamage(kv.Value.ToArray());
            }

            /* you should not call take damage with that method

            foreach (var kv in damages2)
            {
                kv.Key.StartTakeDamage();

                foreach(var d in kv.Value)
                {
                    kv.Key.TakeDamage(d);
                }

                kv.Key.EndTakeDamage();
            }
            */

            // you should use DamageHelper
            //DamageHelper.TakeDamage(damages2);
        }
    }
}