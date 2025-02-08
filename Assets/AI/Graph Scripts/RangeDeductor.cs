using UnityEngine;

public class RangeDetector : MonoBehaviour
{
    public float attackRange = 3f; // Adjustable attack range
    public float hearingRange = 10f; // Adjustable hearing range
    public LayerMask targetLayer;
    public LayerMask enemyLayer;// Layer to detect (e.g., Player)
    private GameObject target;
    private GameObject deadZombie;

    private void Update()
    {
        // Detect targets within the attack range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, targetLayer);

        if (hitColliders.Length > 0)
        {
            target = hitColliders[0].gameObject; // Assume the first target is the one to attack
        }
        else
        {
            target = null;
        }
        
    }
    
    public bool IsTargetInHearingRange()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, hearingRange, targetLayer);
        return hitColliders.Length > 0;
    }
    
    public bool IsDeadZombieInHearingRange()
    {
        Debug.Log("Checking for dead zombie in hearing range");
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, hearingRange, enemyLayer);
        if (hitColliders.Length > 0)
        {
            Debug.Log("Dead zombie found in hearing range");
            deadZombie = hitColliders[0].gameObject; 
            return true;
        }
        Debug.Log("No dead zombie found in hearing range");

        return false;
    }
    
    public GameObject GetDeadZombie()
    {
        Debug.Log("Returning dead zombie");
        return deadZombie;
    }

    public bool IsTargetInAttackRange()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, targetLayer);
        return hitColliders.Length > 0;
    }

    // Visualize the ranges in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}