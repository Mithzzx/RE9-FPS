using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.Serialization;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Chase", story: "[Ai] Navigates to [Target]", category: "Action", id: "aa80305a523e01b4796da62e2ffdfd24")]
public partial class ChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ai;
    [SerializeReference] public BlackboardVariable<GameObject> target;
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] public BlackboardVariable<float> chaseRange = new BlackboardVariable<float>(10f);
    [SerializeReference] public BlackboardVariable<float> stoppingDistance = new BlackboardVariable<float>(1.5f);
    [SerializeReference] public BlackboardVariable<float> speed = new BlackboardVariable<float>(3.5f);
    [SerializeReference] public BlackboardVariable<float> acceleration = new BlackboardVariable<float>(4f);
    

    private static readonly int XSpeed = Animator.StringToHash("XSpeed");

    protected override Status OnStart()
    {
        if (ai.Value is null || target.Value is null)
        {
            return Status.Failure;
        }

        if (agent == null || animator == null)
        {
            return Status.Failure;
        }

        agent.Value.stoppingDistance = stoppingDistance;
        agent.Value.speed = speed;
        agent.Value.acceleration = acceleration;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!ai.Value || !target.Value || agent == null)
        {
            return Status.Failure;
        }

        float distanceToTarget = Vector3.Distance(ai.Value.transform.position, target.Value.transform.position);
        if (distanceToTarget <= chaseRange)
        {
            agent.Value.SetDestination(target.Value.transform.position);

            // Update animator parameters
            Vector3 velocity = agent.Value.velocity;
            animator.Value.SetFloat(XSpeed, velocity.magnitude);

            return Status.Running;
        }

        return Status.Success;
    }

    protected override void OnEnd()
    {
        if (animator != null)
        {
            animator.Value.SetFloat(XSpeed, 0);
        }
    }
}