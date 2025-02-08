using System;
using System.Diagnostics;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Debug = UnityEngine.Debug;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Chase", story: "Chase target with vision cone awareness and memory", category: "Action")]
public partial class ChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ai;
    [SerializeReference] public BlackboardVariable<GameObject> target;
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] public BlackboardVariable<RangeDetector> rangeDetector;
    
    [SerializeReference] public BlackboardVariable<float> baseSpeed = new BlackboardVariable<float>(3.5f);
    [SerializeReference] public BlackboardVariable<float> maxSpeed = new BlackboardVariable<float>(7f);
    [SerializeReference] public BlackboardVariable<float> searchDuration = new BlackboardVariable<float>(30f);
    
    private Vector3 currentDestination;
    private bool isSearching;
    private float searchStartTime;
    
    private static readonly int SpeedHash = Animator.StringToHash("XSpeed");

    protected override Status OnStart()
    {
        if (!ValidateReferences()) return Status.Failure;
        
        agent.Value.speed = baseSpeed.Value;
        agent.Value.stoppingDistance = 1.5f;
        isSearching = false;
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!ValidateReferences()) return Status.Failure;

        if (rangeDetector.Value.IsTargetInSight())
        {
            Debug.Log("Target in sight");
            // Direct chase when target is visible
            currentDestination = target.Value.transform.position;
            agent.Value.speed = maxSpeed.Value;
            isSearching = false;
        }
        else if (AIManager.Instance.HasLastKnownPosition && !isSearching)
        {
            Debug.Log("moving to last known position");
            // Move to last known position if not already searching
            currentDestination = AIManager.Instance.LastKnownPlayerPosition;
            agent.Value.speed = baseSpeed.Value;
            isSearching = true;
            searchStartTime = Time.time;
        }
        else if (isSearching && Time.time - searchStartTime > searchDuration.Value)
        {
            Debug.Log("Search timeout");
            // Give up search after duration
            return Status.Failure;
        }
        Debug.Log("Moving to destination");

        agent.Value.SetDestination(currentDestination);
        UpdateAnimator();
        
        // Check if the agent has reached the destination
        if (!agent.Value.pathPending && agent.Value.remainingDistance <= agent.Value.stoppingDistance)
        {
            if (!agent.Value.hasPath || agent.Value.velocity.sqrMagnitude == 0f)
            {
                return Status.Success;
            }
        }

        return Status.Running;
    }

    private bool ValidateReferences()
    {
        return ai.Value != null && target.Value != null && 
               agent.Value != null && animator.Value != null && 
               rangeDetector.Value != null;
    }

    private void UpdateAnimator()
    {
        if (animator.Value != null)
        {
            animator.Value.SetFloat(SpeedHash, agent.Value.velocity.magnitude);
        }
    }

    protected override void OnEnd()
    {
        if (animator.Value != null)
        {
            animator.Value.SetFloat(SpeedHash, 0);
        }
    }
}