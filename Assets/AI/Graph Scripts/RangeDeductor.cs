using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class RangeDetector : MonoBehaviour
{
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float hearingRange = 10f;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private string deadTag = "Dead";

    private readonly Collider[] hitColliderBuffer = new Collider[10]; // Reusable buffer
    private GameObject currentTarget;
    private GameObject currentDeadZombie;
    private Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
    }

    private void Update()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            cachedTransform.position,
            attackRange,
            hitColliderBuffer
        );

        currentTarget = null;
        for (int i = 0; i < hitCount; i++)
        {
            if (hitColliderBuffer[i].CompareTag(targetTag))
            {
                currentTarget = hitColliderBuffer[i].gameObject;
                break;
            }
        }
    }

    public bool IsTargetInHearingRange()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            cachedTransform.position,
            hearingRange,
            hitColliderBuffer
        );

        for (int i = 0; i < hitCount; i++)
        {
            if (hitColliderBuffer[i].CompareTag(targetTag))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsDeadZombieInHearingRange()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            cachedTransform.position,
            hearingRange,
            hitColliderBuffer
        );

        currentDeadZombie = null;
        for (int i = 0; i < hitCount; i++)
        {
            Debug.Log(hitColliderBuffer[i].name+hitColliderBuffer[i].tag);
            if (hitColliderBuffer[i].CompareTag(deadTag))
            {
                currentDeadZombie = hitColliderBuffer[i].gameObject;
                return true;
            }
        }

        return false;
    }

    public GameObject GetDeadZombie() => currentDeadZombie;

    public bool IsTargetInAttackRange() => currentTarget != null;

    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;

        // Attack range
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(position, attackRange);

        // Hearing range
        Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(position, hearingRange);
    }
}