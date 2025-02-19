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
    [SerializeReference] public BlackboardVariable<AISensor> sensor;

    [SerializeReference] public BlackboardVariable<float> stoppingDistance = new BlackboardVariable<float>(1.5f);
    [SerializeReference] public BlackboardVariable<float> speed = new BlackboardVariable<float>(3.5f);
    [SerializeReference] public BlackboardVariable<float> acceleration = new BlackboardVariable<float>(8f);
    [SerializeReference] public BlackboardVariable<float> stuckThreshold = new BlackboardVariable<float>(0.1f);
    [SerializeReference] public BlackboardVariable<float> stuckTimeout = new BlackboardVariable<float>(5f);

    private GameObject deadZombie;
    private static readonly int SpeedHash = Animator.StringToHash("XSpeed");

    protected override Status OnStart()
    {
        if (!sensor?.Value)
            return Status.Failure;

        GameObject[] filteredObjects = new GameObject[1];
        int count = sensor.Value.Filter(filteredObjects, "Dead", false);

        if (count > 0)
        {
            deadZombie = filteredObjects[0];
        }
        Debug.Log($"NavigatingToDeadAction: {count} dead zombies found");
        

        if (!deadZombie) return Status.Failure;

        agent.Value.stoppingDistance = stoppingDistance;
        agent.Value.speed = speed;
        agent.Value.acceleration = acceleration;

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!deadZombie) return Status.Failure;

        float distanceDiff = Vector3.Distance(ai.Value.transform.position, deadZombie.transform.position);

        if (distanceDiff > stoppingDistance)
        {
            agent.Value.SetDestination(deadZombie.transform.position);
            animator.Value.SetFloat(SpeedHash, agent.Value.velocity.magnitude / agent.Value.speed);
            distanceDiff = Vector3.Distance(ai.Value.transform.position, deadZombie.transform.position);
            return Status.Running;
        }

        return Status.Success;
    }

    protected override void OnEnd()
    {
        if (agent.Value)
        {
            agent.Value.ResetPath();
        }

        if (animator.Value)
        {
            animator.Value.SetFloat(SpeedHash, 0f);
        }

        if (deadZombie && ai.Value)
        {
            ai.Value.transform.LookAt(deadZombie.transform);
        }
    }
}