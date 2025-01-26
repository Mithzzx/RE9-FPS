using UnityEngine;

public class Grenade : MonoBehaviour
{
    [Header("Explosion Prefab")]
    [SerializeReference] private GameObject explosionPrefab;
    [SerializeField] private Vector3 explosionOffset;
    
    [Header("Settings")]
    [SerializeField] private float explosionDelay = 3f;
    [SerializeField] private float explosionForce = 700f;
    [SerializeField] private float explosionRadius = 5f;
    
    [Header("Audio Effects")]
    
    private float countdown;
    private bool hasExploded = false;
    
    private void Start()
    {
        countdown = explosionDelay;
    }
    
    private void Update()
    {
        if (!hasExploded)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0)
            {
                Explode();
                hasExploded = true;
            }
        }
    }

    private void Explode()
    {
        GameObject explosion = Instantiate(explosionPrefab, transform.position + explosionOffset, Quaternion.identity);
        
        Destroy(explosion, 1.8f);
        
        // Audio
        
        // Affect nearby objects
        NearbyForceApply();
        
        Destroy(gameObject);
    }

    private void NearbyForceApply()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        
        foreach (Collider nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }
    }
}
