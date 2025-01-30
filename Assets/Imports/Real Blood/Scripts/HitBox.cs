using System;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Simple hitbox with TakeDamage Event
    /// </summary>
    public class HitBox : MonoBehaviour, IHittable
    {
        private Action<DamageData[]> onDamageEvent;

        /// <summary>
        /// Take Damage (call event)
        /// </summary>
        /// <param name="damage"></param>
        public void TakeDamage(DamageData[] damage)
        {
            onDamageEvent(damage);
        }

        /// <summary>
        /// Subscribe on Damage event
        /// </summary>
        /// <param name="callback"></param>
        public void AddListener(Action<DamageData[]> callback)
        {
            onDamageEvent += callback;
        }

        /// <summary>
        /// Unsubscribe from Damage event
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveListener(Action<DamageData[]> callback)
        {
            onDamageEvent -= callback;
        }
    }
}