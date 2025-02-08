using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;
using UnityEngine.Serialization;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Navigating to DeadZombie", story: "AI Navigate to DeadZombie in Range", 
    category: "Action", id: "b0d12bebfe40b3d1133ae2f30b71b764")]
public partial class NavigatingToDeadZombieAction : Action
{
    [SerializeReference] private BlackboardVariable<GameObject> ai;
    [SerializeReference] private BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] private BlackboardVariable<Animator> animator;
    [SerializeReference] private BlackboardVariable<RangeDetector> range;

    private GameObject deadZombie;
    private static readonly int XSpeedHash = Animator.StringToHash("XSpeed");
    private const float StoppingDistance = 0.8f;

    protected override Status OnStart()
    {
        var detector = range.Value;
        if (detector == null) return Status.Failure;

        deadZombie = detector.GetDeadZombie();
        return deadZombie != null ? Status.Running : Status.Failure;
    }

    protected override Status OnUpdate()
    {
        if (deadZombie == null) return Status.Failure;

        var currentAgent = agent.Value;
        var currentAnimator = animator.Value;
        
        if (currentAgent == null || currentAnimator == null) 
            return Status.Failure;

        float distanceToTarget = Vector3.Distance(
            ai.Value.transform.position, 
            deadZombie.transform.position
        );

        if (distanceToTarget <= StoppingDistance)
            return Status.Success;

        currentAgent.SetDestination(deadZombie.transform.position);
        currentAnimator.SetFloat(XSpeedHash, 
            currentAgent.velocity.magnitude / currentAgent.speed);
        
        return Status.Running;
    }

    protected override void OnEnd() { }
}
