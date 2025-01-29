using UnityEngine;

public class HitBox : MonoBehaviour
{
    public Health health;

    public void OnRaycastHit(GunMechanics gun, Vector3 direction)
    {
        health.TakeDamage(gun.damage, direction);
    }
        
}
