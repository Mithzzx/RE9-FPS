using UnityEngine;

public class RangeDetector : MonoBehaviour
{
    [Header("Detection Ranges")]
    public float attackRange = 3f;
    public float hearingRange = 10f;
    public float visionRange = 15f;
    public float visionAngle = 45f;
    
    [Header("Tags and Layers")]
    public string targetTag = "Player";
    public string deadZombieTag = "DeadZombie";
    public LayerMask obstacleLayer;
    
    [Header("Performance Settings")]
    public float visionCheckInterval = 0.2f;
    public float hearingCheckInterval = 0.5f;

    private float nextVisionCheck;
    private float nextHearingCheck;
    private GameObject target;
    private GameObject deadZombie;
    private bool targetInSight;
    private bool deadZombieInRange;
    private Transform cachedTransform;
    private static readonly Collider[] HitColliders = new Collider[20];

    private void Awake()
    {
        cachedTransform = transform;
        obstacleLayer = LayerMask.GetMask("Obstacle", "Wall");
    }

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag(targetTag);
        nextVisionCheck = nextHearingCheck = 0f;
    }

    private void Update()
    {
        float currentTime = Time.time;
        
        if (currentTime >= nextVisionCheck)
        {
            CheckVision();
            nextVisionCheck = currentTime + visionCheckInterval;
        }

        if (currentTime >= nextHearingCheck)
        {
            CheckHearing();
            nextHearingCheck = currentTime + hearingCheckInterval;
        }
    }

    private void CheckVision()
    {
        if (target == null) return;

        Vector3 directionToTarget = target.transform.position - cachedTransform.position;
        float distanceToTarget = directionToTarget.magnitude;

        targetInSight = distanceToTarget <= visionRange &&
                       Vector3.Angle(cachedTransform.forward, directionToTarget) <= visionAngle / 2 &&
                       !Physics.Raycast(cachedTransform.position, directionToTarget.normalized, 
                           distanceToTarget, obstacleLayer);

        if (targetInSight)
        {
            AIManager.Instance.UpdateLastKnownPosition(target.transform.position);
        }
    }

    private void CheckHearing()
    {
        int numColliders = Physics.OverlapSphereNonAlloc(
            cachedTransform.position, 
            hearingRange,
            HitColliders);

        deadZombieInRange = false;
        deadZombie = null;

        for (int i = 0; i < numColliders; i++)
        {
            if (HitColliders[i].CompareTag(deadZombieTag))
            {
                deadZombieInRange = true;
                deadZombie = HitColliders[i].gameObject;
                break;
            }
        }
    }

    public bool IsTargetInSight() => targetInSight;
    public bool IsDeadZombieInRange() => deadZombieInRange;
    public GameObject GetDeadZombie() => deadZombie;

    public bool IsTargetInAttackRange()
    {
        return target != null && 
               Vector3.Distance(cachedTransform.position, target.transform.position) <= attackRange;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        Vector3 forward = transform.forward;

        // Vision cone
        Gizmos.color = targetInSight ? Color.green : Color.yellow;
        Vector3 forwardRay = forward * visionRange;
        Vector3 leftRay = Quaternion.Euler(0, -visionAngle / 2, 0) * forwardRay;
        Vector3 rightRay = Quaternion.Euler(0, visionAngle / 2, 0) * forwardRay;
        
        Gizmos.DrawLine(position, position + leftRay);
        Gizmos.DrawLine(position, position + rightRay);
        
        // Ranges
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position, attackRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(position, hearingRange);
        
        // Last known position
        if (AIManager.Instance?.HasLastKnownPosition == true)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(AIManager.Instance.LastKnownPlayerPosition, 1f);
        }
    }
}