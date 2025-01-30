using System.Collections;
using System.Collections.Generic;

namespace Knife.RealBlood
{
    /// <summary>
    /// Simple Hittable object interface
    /// </summary>
    public interface IHittable
    {
        void TakeDamage(DamageData[] damage);
    }
}