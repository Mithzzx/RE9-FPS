using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private GameObject deadZombie;
    [SerializeField] private int maxHealth = 100;
    public float currentHealth;
    
    public bool IsDead => currentHealth <= 0;
    
    private Ragdoll ragdoll;
    Animator animator;
    
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
        deadZombie.SetActive(true);
        ragdoll.EnableRagdoll();
    }
}
