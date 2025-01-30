using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood.LiquidSurface
{
    /// <summary>
    /// Liquid surface call SpawnParticle on TakeDamage
    /// </summary>
    [RequireComponent(typeof(LiquidSurface))]
    public class LiquidSurfaceParticleFromDamage : MonoBehaviour, IHittable
    {
        private LiquidSurface surface;

        private void Start()
        {
            surface = GetComponent<LiquidSurface>();
        }

        public void TakeDamage(DamageData[] damage)
        {
            for (int i = 0; i < damage.Length; i++)
            {
                surface.SpawnParticle(damage[i].point);
            }
        }
    }
}