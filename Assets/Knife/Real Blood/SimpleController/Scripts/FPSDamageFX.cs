using UnityEngine;

namespace Knife.RealBlood.SimpleController
{
    /// <summary>
    /// FPSDamageFX behaviour can playFX on Damage events
    /// </summary>
    public abstract class FPSDamageFX : MonoBehaviour, IFPSDamageFX
    {
        public abstract void PlayFX(DamageData[] damage);
    }
}