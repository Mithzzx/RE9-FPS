using UnityEngine;
using UnityEngine.AI;

public class AILocomotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    private NavMeshAgent agent;
    private Animator animator;
    
    [Header("Settings")]
    [SerializeField] private float maxDistance = 10f;
    
    private static readonly int Speed = Animator.StringToHash("Speed");
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, target.position) < maxDistance)
        {
            agent.SetDestination(target.position);
        }
        animator.SetFloat(Speed, agent.velocity.magnitude);
    }
}
