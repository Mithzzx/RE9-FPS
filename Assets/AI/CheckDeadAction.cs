using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckDead", story: "Check for Dead Zombie in [Range]", category: "Action", id: "42d459001e25c442171041a8b416b8bf")]
public partial class CheckDeadAction : Action
{
    [SerializeReference] public BlackboardVariable<RangeDetector> Range;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] public BlackboardVariable<GameObject> ai;
    [SerializeField] private LayerMask deadZombieLayer;
    [SerializeField, Range(0, 100)] private float moveToDeadZombiePercentage = 20f;

    private GameObject deadZombie;
    private System.Random random = new System.Random();
    private bool shouldMove;

    protected override Status OnStart()
    {
        deadZombieLayer = ai.Value.layer;
        if (Range.Value == null || agent.Value == null || ai.Value == null)
        {
            return Status.Failure;
        }

        shouldMove = random.NextDouble() <= moveToDeadZombiePercentage / 100f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (shouldMove)
        {
            Collider[] hitColliders = Physics.OverlapSphere(ai.Value.transform.position, Range.Value.hearingRange, deadZombieLayer);
            if (hitColliders.Length > 0)
            {
                deadZombie = hitColliders[0].gameObject;
                agent.Value.SetDestination(deadZombie.transform.position);
                return Status.Running;
            }
        }

        return Status.Success;
    }

    protected override void OnEnd()
    {
        if (agent.Value != null)
        {
            agent.Value.ResetPath();
        }
    }
}