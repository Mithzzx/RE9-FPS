using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Navigate to Dead Zombie", story: "AI navigates to nearby dead zombie", id: "42981fe59ebe0fdcadaef6a8de88b70a")]
public partial class NavigatingToDeadAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ai;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<RangeDetector> range;

    [SerializeReference] public BlackboardVariable<float> stoppingDistance = new BlackboardVariable<float>(1.5f);
    [SerializeReference] public BlackboardVariable<float> speed = new BlackboardVariable<float>(3.5f);
    [SerializeReference] public BlackboardVariable<float> acceleration = new BlackboardVariable<float>(8f);
    [SerializeReference] public BlackboardVariable<float> stuckThreshold = new BlackboardVariable<float>(0.1f);
    [SerializeReference] public BlackboardVariable<float> stuckTimeout = new BlackboardVariable<float>(5f);

    private GameObject deadZombie;
    private float previousDistance;
    private static readonly int SpeedHash = Animator.StringToHash("XSpeed");

    protected override Status OnStart()
    {
        if (range?.Value == null || !range.Value.IsDeadZombieInRange())
            return Status.Failure;

        deadZombie = range.Value.GetDeadZombie();
        if (deadZombie == null) return Status.Failure;

        previousDistance = float.MaxValue;

        agent.Value.stoppingDistance = stoppingDistance;
        agent.Value.speed = speed;
        agent.Value.acceleration = acceleration;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (deadZombie == null) return Status.Failure;

        float currentDistance = Vector3.Distance(ai.Value.transform.position, deadZombie.transform.position);
        

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