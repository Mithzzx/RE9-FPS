using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;
using UnityEngine.Serialization;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Navigating to DeadZombie", story: "AI Navigate to DeadZombie in [Range]", category: "Action", id: "b0d12bebfe40b3d1133ae2f30b71b764")]
public partial class NavigatingToDeadZombieAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ai;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [FormerlySerializedAs("Range")] [SerializeReference] public BlackboardVariable<RangeDetector> range;
    private GameObject deadZombie;

    protected override Status OnStart()
    {
        if (range.Value == null)
        {
            return Status.Failure;
        }
        Debug.Log("Getting to DeadZombie");
        deadZombie = range.Value.GetDeadZombie();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        float distanceToTarget = Vector3.Distance(ai.Value.transform.position, deadZombie.transform.position);
        if (distanceToTarget > 0.8f)
        {
            agent.Value.SetDestination(deadZombie.transform.position);
            Vector3 velocity = agent.Value.velocity;
            animator.Value.SetFloat(Animator.StringToHash("XSpeed"), velocity.magnitude/agent.Value.speed);
            return Status.Running;
        }
        
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

