using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Smart Attack", story: "Coordinate with other zombies to attack strategically", category: "Action", id: "72457a52db1bd84bbe5da50724c01435")]
public partial class AttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ai;
    [SerializeReference] public BlackboardVariable<GameObject> target;
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] public BlackboardVariable<RangeDetector> rangeDetector;

    [Header("Attack Settings")]
    [SerializeField] private float circleRadius = 5f;
    [SerializeField] private float attackDuration = 1.5f;
    [SerializeField] private float minAttackDistance = 2f;
    [SerializeField] private float maxAttackDistance = 3f;

    private enum AttackState { Circling, Positioning, Attacking, Retreating }
    private AttackState currentState;
    private float stateTimer;
    private Vector3 attackPosition;
    private static readonly int SpeedHash = Animator.StringToHash("XSpeed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    protected override Status OnStart()
    {
        if (!ValidateReferences()) return Status.Failure;

        AIManager.Instance.RegisterAttacker(this);
        currentState = AttackState.Circling;
        agent.Value.stoppingDistance = 0.5f;
        Debug.Log("AttackAction started: Circling state");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!ValidateReferences()) return Status.Failure;

        switch (currentState)
        {
            case AttackState.Circling:
                return HandleCircling();
            case AttackState.Positioning:
                return HandlePositioning();
            case AttackState.Attacking:
                return HandleAttacking();
            case AttackState.Retreating:
                return HandleRetreating();
            default:
                return Status.Failure;
        }
    }

    private Status HandleCircling()
    {
        Vector3 circlePos = AIManager.Instance.GetCirclingPosition(ai.Value, target.Value, circleRadius);
        agent.Value.SetDestination(circlePos);
        Debug.Log("Circling around target");

        if (AIManager.Instance.CanAttack(this, target.Value.transform))
        {
            currentState = AttackState.Positioning;
            Vector3 dirToPlayer = (target.Value.transform.position - ai.Value.transform.position).normalized;
            float randomAngle = UnityEngine.Random.Range(-90f, 90f);
            Vector3 attackDir = Quaternion.Euler(0, randomAngle, 0) * dirToPlayer;
            attackPosition = target.Value.transform.position + attackDir * UnityEngine.Random.Range(minAttackDistance, maxAttackDistance);
            Debug.Log("Switching to Positioning state");
        }

        UpdateAnimator(true);
        return Status.Running;
    }

    private Status HandlePositioning()
    {
        agent.Value.SetDestination(attackPosition);
        Debug.Log("Positioning for attack");

        Debug.DrawRay(ai.Value.transform.position, attackPosition - ai.Value.transform.position, Color.red);
        Debug.Log(Vector3.Distance(ai.Value.transform.position, attackPosition));
        if (Vector3.Distance(ai.Value.transform.position, attackPosition) < 9f)
        {
            currentState = AttackState.Attacking;
            stateTimer = attackDuration;
            animator.Value.SetTrigger(AttackHash);
            AIManager.Instance.RecordAttack(this);
            Debug.Log("Switching to Attacking state");
        }

        UpdateAnimator(true);
        return Status.Running;
    }

    private Status HandleAttacking()
    {
        Vector3 dirToTarget = target.Value.transform.position - ai.Value.transform.position;
        dirToTarget.y = 0;
        ai.Value.transform.rotation = Quaternion.LookRotation(dirToTarget);
        Debug.Log("Attacking target");

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            currentState = AttackState.Retreating;
            Debug.Log("Switching to Retreating state");
        }

        return Status.Running;
    }

    private Status HandleRetreating()
    {
        Vector3 retreatPos = ai.Value.transform.position + (ai.Value.transform.position - target.Value.transform.position).normalized * circleRadius;
        agent.Value.SetDestination(retreatPos);
        Debug.Log("Retreating from target");

        if (Vector3.Distance(ai.Value.transform.position, target.Value.transform.position) >= circleRadius)
        {
            currentState = AttackState.Circling;
            Debug.Log("Switching to Circling state");
        }

        UpdateAnimator(true);
        return Status.Running;
    }

    private bool ValidateReferences()
    {
        return ai.Value != null && target.Value != null && agent.Value != null && animator.Value != null && rangeDetector.Value != null;
    }

    private void UpdateAnimator(bool moving)
    {
        if (animator.Value != null)
        {
            animator.Value.SetFloat(SpeedHash, moving ? agent.Value.velocity.magnitude : 0);
        }
    }

    protected override void OnEnd()
    {
        if (animator.Value != null)
        {
            animator.Value.SetFloat(SpeedHash, 0);
        }
        AIManager.Instance.UnregisterAttacker(this);
        Debug.Log("AttackAction ended");
    }
}