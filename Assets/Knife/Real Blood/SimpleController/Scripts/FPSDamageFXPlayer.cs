using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Knife.RealBlood.SimpleController
{
    /// <summary>
    /// FPSDamageFXPlayer play selected FPSDamageFX behaviours
    /// </summary>
    public class FPSDamageFXPlayer : MonoBehaviour, IHittable
    {
        [SerializeField] private FPSDamageFX[] effects;
        [Header("Monobehaviours with IFPSDamageFX implemented interface")]
        [SerializeField] private MonoBehaviour[] monoEffects;

        private IFPSDamageFX[] fPSDamageFXes;

        private void Awake()
        {
            fPSDamageFXes = monoEffects.ToList().ConvertAll(m => m as IFPSDamageFX).ToArray();
        }

        private void OnValidate()
        {
            for (int i = 0; i < monoEffects.Length; i++)
            {
                IFPSDamageFX fPSDamageFX = monoEffects[i].GetComponent<IFPSDamageFX>();

                if (fPSDamageFX == null)
                {
                    monoEffects[i] = null;
                }
                else
                {
                    monoEffects[i] = fPSDamageFX as MonoBehaviour;
                }
            }
        }

        public void TakeDamage(DamageData[] damage)
        {
            foreach (var f in effects)
            {
                f.PlayFX(damage);
            }
            foreach (var f in fPSDamageFXes)
            {
                f.PlayFX(damage);
            }
        }
    }
}