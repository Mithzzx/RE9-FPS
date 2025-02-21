using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SmartChase", story: "Intelligently chases target with position prediction and random path variation", category: "Action")]
public partial class ChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ai;
    [SerializeReference] public BlackboardVariable<GameObject> target;
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] public BlackboardVariable<RangeDetector> rangeDetector;
    
    [SerializeReference] public BlackboardVariable<float> speed = new BlackboardVariable<float>(3f);
    [SerializeReference] public BlackboardVariable<float> acceleration = new BlackboardVariable<float>(8f);
    [SerializeReference] public BlackboardVariable<float> stoppingDistance = new BlackboardVariable<float>(0.5f);
    
    // Prediction variables
    [SerializeReference] public BlackboardVariable<float> predictionTime = new BlackboardVariable<float>(0.5f);
    [SerializeReference] public BlackboardVariable<float> maxPredictionDistance = new BlackboardVariable<float>(5f);
    
    // Path randomization variables - adjusted for more noticeable random movement
    [SerializeReference] public BlackboardVariable<float> pathRandomizationRadius = new BlackboardVariable<float>(3f); // Increased radius
    [SerializeReference] public BlackboardVariable<float> pathUpdateInterval = new BlackboardVariable<float>(0.75f);
    [SerializeReference] public BlackboardVariable<int> waypointsCount = new BlackboardVariable<int>(3); // More waypoints
    
    // New variables to control behavior switching
    [SerializeReference] public BlackboardVariable<float> directPursuitThreshold = new BlackboardVariable<float>(2f); // Distance threshold for direct pursuit
    
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private float lastUpdateTime;
    private float lastPathUpdateTime;
    private Vector3[] randomWaypoints;
    private int currentWaypointIndex;
    private bool isDirectPursuit;
    private bool hasReachedTarget;
    private float nextPathUpdateTime;
    
    private static readonly int XSpeed = Animator.StringToHash("XSpeed");
    
    protected override Status OnStart()
    {
        if (!ValidateReferences())
            return Status.Failure;
            
        // Initialize tracking variables
        lastTargetPosition = target.Value.transform.position;
        lastUpdateTime = Time.time;
        lastPathUpdateTime = Time.time;
        nextPathUpdateTime = Time.time;
        targetVelocity = Vector3.zero;
        isDirectPursuit = false; // Start with random path movement
        hasReachedTarget = false;
        
        // Initialize waypoints
        randomWaypoints = new Vector3[waypointsCount.Value];
        
        // Configure NavMeshAgent
        ConfigureAgent();
        
        // Generate initial random path
        GenerateRandomPath();
            
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!ValidateReferences())
            return Status.Failure;

        float distanceToTarget = Vector3.Distance(ai.Value.transform.position, target.Value.transform.position);

        // Update target velocity calculation
        UpdateTargetVelocity();

        // Check if we've reached the target
        if (distanceToTarget <= stoppingDistance.Value)
        {
            hasReachedTarget = true;
            return Status.Success;
        }

        // Only switch to direct pursuit when very close or target is moving very fast
        bool shouldDirectPursue = distanceToTarget < directPursuitThreshold.Value;

        if (shouldDirectPursue && !isDirectPursuit)
        {
            isDirectPursuit = true;
            agent.Value.SetDestination(CalculatePredictedPosition());
        }
        else if (!shouldDirectPursue && isDirectPursuit)
        {
            isDirectPursuit = false;
            GenerateRandomPath();
        }

        if (!isDirectPursuit)
        {
            // Update random path periodically
            if (Time.time >= nextPathUpdateTime)
            {
                GenerateRandomPath();
                nextPathUpdateTime = Time.time + pathUpdateInterval.Value;
            }

            UpdatePathProgress();
        }
        else
        {
            agent.Value.SetDestination(CalculatePredictedPosition());
        }

        // Ensure the agent is actually moving
        EnsureAgentMovement();

        // Update animator parameters
        UpdateAnimator();

        return Status.Running;
    }

    private void GenerateRandomPath()
    {
        Vector3 predictedTargetPos = CalculatePredictedPosition();
        Vector3 directionToTarget = (predictedTargetPos - ai.Value.transform.position).normalized;
        
        // Generate random waypoints with more variation
        for (int i = 0; i < randomWaypoints.Length; i++)
        {
            // Calculate base position along the direct path with more spread
            float progress = (i + 1.0f) / (randomWaypoints.Length + 1.0f);
            Vector3 basePos = Vector3.Lerp(ai.Value.transform.position, predictedTargetPos, progress);
            
            // Add random offset in both perpendicular directions
            Vector3 perpendicular = Vector3.Cross(directionToTarget, Vector3.up);
            float randomOffset1 = Random.Range(-pathRandomizationRadius.Value, pathRandomizationRadius.Value);
            float randomOffset2 = Random.Range(-pathRandomizationRadius.Value * 0.5f, pathRandomizationRadius.Value * 0.5f);
            Vector3 randomPos = basePos + (perpendicular * randomOffset1) + (Vector3.up * randomOffset2);
            
            // Ensure point is on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, pathRandomizationRadius.Value, NavMesh.AllAreas))
            {
                randomWaypoints[i] = hit.position;
            }
            else
            {
                // If no valid position found, try a position closer to the base path
                randomPos = basePos + (perpendicular * (randomOffset1 * 0.5f));
                if (NavMesh.SamplePosition(randomPos, out hit, pathRandomizationRadius.Value * 0.5f, NavMesh.AllAreas))
                {
                    randomWaypoints[i] = hit.position;
                }
                else
                {
                    randomWaypoints[i] = basePos; // Fallback to base position
                }
            }
        }
        
        currentWaypointIndex = 0;
        
        // Set initial destination
        if (randomWaypoints.Length > 0)
        {
            agent.Value.SetDestination(randomWaypoints[0]);
        }
    }

    private void UpdatePathProgress()
    {
        if (currentWaypointIndex >= randomWaypoints.Length)
        {
            GenerateRandomPath();
            return;
        }
            
        // Check if we've reached the current waypoint
        float distanceToWaypoint = Vector3.Distance(ai.Value.transform.position, randomWaypoints[currentWaypointIndex]);
        if (distanceToWaypoint <= agent.Value.stoppingDistance + 0.5f)
        {
            currentWaypointIndex++;
            
            // If there are more waypoints, set the next destination
            if (currentWaypointIndex < randomWaypoints.Length)
            {
                agent.Value.SetDestination(randomWaypoints[currentWaypointIndex]);
            }
            else
            {
                GenerateRandomPath();
            }
        }
    }

    private void UpdateTargetVelocity()
    {
        float deltaTime = Time.time - lastUpdateTime;
        if (deltaTime > 0)
        {
            Vector3 currentTargetPosition = target.Value.transform.position;
            targetVelocity = (currentTargetPosition - lastTargetPosition) / deltaTime;
            lastTargetPosition = currentTargetPosition;
            lastUpdateTime = Time.time;
        }
    }

    private void EnsureAgentMovement()
    {
        // If agent is stuck, regenerate path
        if (agent.Value.velocity.magnitude < 0.1f && !hasReachedTarget)
        {
            agent.Value.ResetPath();
            Vector3 destination = isDirectPursuit ? 
                CalculatePredictedPosition() : 
                randomWaypoints[currentWaypointIndex];
            agent.Value.SetDestination(destination);
        }
    }

    private void ConfigureAgent()
    {
        agent.Value.speed = speed.Value;
        agent.Value.acceleration = acceleration.Value;
        agent.Value.stoppingDistance = stoppingDistance.Value;
        agent.Value.autoBraking = true; // Enable auto-braking for better control
        agent.Value.autoRepath = true; // Enable automatic path recalculation
    }

    private void UpdateAnimator()
    {
        if (animator.Value != null)
        {
            Vector3 velocity = agent.Value.velocity;
            float speedRatio = velocity.magnitude / agent.Value.speed;
            animator.Value.SetFloat(XSpeed, speedRatio * 2f);
        }
    }


    protected override void OnEnd()
    {
        if (agent.Value != null && agent.Value.isOnNavMesh)
        {
            agent.Value.ResetPath();
        }
        
        // Reset velocities if we reached the target
        if (hasReachedTarget && agent.Value != null)
        {
            agent.Value.velocity = Vector3.zero;
        }
    }
    
    private Vector3 CalculatePredictedPosition()
    {
        Vector3 currentPosition = target.Value.transform.position;
        
        // Calculate base prediction
        float distanceToTarget = Vector3.Distance(ai.Value.transform.position, currentPosition);
        float predictionTimeScale = Mathf.Clamp(distanceToTarget / agent.Value.speed, 0, predictionTime.Value);
        Vector3 prediction = currentPosition + (targetVelocity * predictionTimeScale);
        
        // Limit prediction distance
        Vector3 predictionOffset = prediction - currentPosition;
        if (predictionOffset.magnitude > maxPredictionDistance.Value)
        {
            prediction = currentPosition + (predictionOffset.normalized * maxPredictionDistance.Value);
        }
        
        // Validate prediction is on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(prediction, out hit, 1.0f, NavMesh.AllAreas))
        {
            prediction = hit.position;
        }
        
        return prediction;
    }
    
    private bool ValidateReferences()
    {
        return ai.Value && 
               target.Value && 
               agent.Value;
    }
}