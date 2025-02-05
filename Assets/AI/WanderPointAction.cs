using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wander Point", story: "[Ai] Moves to Random Wander", category: "Action", id: "26ea1cbb78774771f5e142423ccd2743")]
public partial class WanderPointAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Ai;
    private BlackboardVariable<GameObject> WanderPoint;
    [SerializeReference] public BlackboardVariable<float> minRange = new BlackboardVariable<float>(5f);
    [SerializeReference] public BlackboardVariable<float> maxRange = new BlackboardVariable<float>(10f);
    [SerializeReference] public BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<float> speed = new BlackboardVariable<float>(0.3f);
    [SerializeReference] public BlackboardVariable<float> acceleration = new BlackboardVariable<float>(0.08f);
    [SerializeReference] public BlackboardVariable<float> stoppingDistance = new BlackboardVariable<float>(0f);

    private Vector3 wanderPosition;

    private static readonly int XSpeed = Animator.StringToHash("XSpeed");
    private static readonly int YSpeed = Animator.StringToHash("YSpeed");

    protected override Status OnStart()
    {
        if (Ai.Value == null)
        {
            return Status.Failure;
        }

        if (agent == null || animator == null)
        {
            return Status.Failure;
        }

        // Generate a random position within the specified range
        float randomDistance = UnityEngine.Random.Range(minRange, maxRange);
        float randomAngle = UnityEngine.Random.Range(0f, 360f);
        Vector3 offset = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)) * randomDistance;
        wanderPosition = Ai.Value.transform.position + offset;

        // Instantiate the wander point at the calculated position
        WanderPoint = new BlackboardVariable<GameObject>(new GameObject("WanderPoint"));
        WanderPoint.Value.transform.position = wanderPosition;
        
        // Set the NavMeshAgent properties
        agent.Value.stoppingDistance = stoppingDistance;
        agent.Value.speed = speed;
        agent.Value.acceleration = acceleration;
        
        // Set the destination for the NavMeshAgent
        agent.Value.SetDestination(wanderPosition);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Ai.Value == null || WanderPoint.Value == null || agent == null)
        {
            return Status.Failure;
        }

        // Update animator parameters
        Vector3 velocity = agent.Value.velocity;
        animator.Value.SetFloat(XSpeed, velocity.magnitude/agent.Value.speed);

        // Check if Ai has reached the wander point
        if (!agent.Value.pathPending && agent.Value.remainingDistance < 0.1f)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (animator != null)
        {
            animator.Value.SetFloat(XSpeed, 0);
        }
        if (WanderPoint.Value != null)
        {
            GameObject.Destroy(WanderPoint.Value);
        }
    }
}