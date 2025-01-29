using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private float currentHealth;
    
    private Ragdoll ragdoll;
    
    private void Start()
    {
        currentHealth = maxHealth;
        ragdoll = GetComponent<Ragdoll>();
        
        var rigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies)
        {
            HitBox hitBox = rb.gameObject.AddComponent<HitBox>();
            hitBox.health = this;
        }
    }
    
    public void TakeDamage(float damage, Vector3 direction)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        ragdoll.EnableRagdoll();
    }
}
