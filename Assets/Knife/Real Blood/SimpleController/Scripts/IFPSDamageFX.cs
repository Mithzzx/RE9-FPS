
namespace Knife.RealBlood.SimpleController
{
    /// <summary>
    /// FPSDamageFX interface to play some FX on damage events
    /// </summary>
    public interface IFPSDamageFX
    {
        void PlayFX(DamageData[] damage);
    }
}