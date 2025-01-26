using UnityEngine;

public class GrenadeThrow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputHandler input;
    [Header("Grenade Prefab")]
    [SerializeField] private GameObject grenadePrefab;
    
    [Header("Settings")]
    [SerializeField] private bool canThrow = true;
    [SerializeField] private Transform grenadeSpawnPoint;
    [SerializeField] private float throwForce = 40f;

    // Update is called once per frame
    void Update()
    {
        if (canThrow && input.AttackTriggered)
        {
            ThrowGrenade();
        }
    }

    private void ThrowGrenade()
    {
        GameObject grenade = Instantiate(grenadePrefab, grenadeSpawnPoint.position, grenadeSpawnPoint.rotation);
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        rb.AddForce(grenadeSpawnPoint.forward * throwForce, ForceMode.VelocityChange);
    }
}
