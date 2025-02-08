using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Navigate to Dead Zombie", story: "AI navigates to nearby dead zombie")]
public partial class NavigatingToDeadZombieAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ai;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<RangeDetector> range;
    
    [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private float stuckThreshold = 0.1f;
    [SerializeField] private float stuckTimeout = 3f;
    
    private GameObject deadZombie;
    private float previousDistance;
    private float stuckTimer;
    private static readonly int SpeedHash = Animator.StringToHash("XSpeed");
    
    protected override Status OnStart()
    {
        if (range?.Value == null || !range.Value.IsDeadZombieInRange())
            return Status.Failure;
            
        deadZombie = range.Value.GetDeadZombie();
        if (deadZombie == null) return Status.Failure;
        
        previousDistance = float.MaxValue;
        stuckTimer = 0f;
        
        agent.Value.stoppingDistance = stoppingDistance;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (deadZombie == null) return Status.Failure;

        float currentDistance = Vector3.Distance(ai.Value.transform.position, deadZombie.transform.position);
        
        // Check if stuck
        if (Mathf.Abs(currentDistance - previousDistance) < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckTimeout)
                return Status.Failure;
        }
        else
        {
            stuckTimer = 0f;
        }

        if (currentDistance > stoppingDistance)
        {
            agent.Value.SetDestination(deadZombie.transform.position);
            animator.Value.SetFloat(SpeedHash, agent.Value.velocity.magnitude / agent.Value.speed);
            previousDistance = currentDistance;
            return Status.Running;
        }

        return Status.Success;
    }

    protected override void OnEnd()
    {
        if (agent.Value != null)
        {
            agent.Value.ResetPath();
        }
        
        if (animator.Value != null)
        {
            animator.Value.SetFloat(SpeedHash, 0f);
        }
        
        if (deadZombie != null && ai.Value != null)
        {
            ai.Value.transform.LookAt(deadZombie.transform);
        }
    }
}