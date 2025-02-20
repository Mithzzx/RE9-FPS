using System;
using System.Diagnostics;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Debug = UnityEngine.Debug;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SmartChase", story: "Intelligently chases target with position prediction", category: "Action")]
public partial class ChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ai;
    [SerializeReference] public BlackboardVariable<GameObject> target;
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> agent;
    [SerializeReference] public BlackboardVariable<RangeDetector> rangeDetector;
    
    [SerializeReference] public BlackboardVariable<float> speed = new BlackboardVariable<float>(0.3f);
    [SerializeReference] public BlackboardVariable<float> acceleration = new BlackboardVariable<float>(0.08f);
    [SerializeReference] public BlackboardVariable<float> stoppingDistance = new BlackboardVariable<float>(0.5f);
    
    // New variables for prediction
    [SerializeReference] public BlackboardVariable<float> predictionTime = new BlackboardVariable<float>(0.5f);
    [SerializeReference] public BlackboardVariable<float> maxPredictionDistance = new BlackboardVariable<float>(5f);
    
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private float lastUpdateTime;
    
    private static readonly int XSpeed = Animator.StringToHash("XSpeed");
    
    protected override Status OnStart()
    {
        if (!ValidateReferences())
            return Status.Failure;
            
        // Initialize tracking variables
        lastTargetPosition = target.Value.transform.position;
        lastUpdateTime = Time.time;
        targetVelocity = Vector3.zero;
        
        // Configure NavMeshAgent
        agent.Value.speed = speed.Value;
        agent.Value.acceleration = acceleration.Value;
        agent.Value.stoppingDistance = stoppingDistance.Value;
            
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!ValidateReferences())
            return Status.Failure;

        // Update target velocity calculation
        float deltaTime = Time.time - lastUpdateTime;
        if (deltaTime > 0)
        {
            Vector3 currentTargetPosition = target.Value.transform.position;
            targetVelocity = (currentTargetPosition - lastTargetPosition) / deltaTime;
            lastTargetPosition = currentTargetPosition;
            lastUpdateTime = Time.time;
        }

        // Calculate predicted position
        Vector3 predictedPosition = CalculatePredictedPosition();
        
        // Set destination for NavMeshAgent
        agent.Value.SetDestination(predictedPosition);

        // Update animator parameters based on actual movement
        Vector3 velocity = agent.Value.velocity;
        animator.Value.SetFloat(XSpeed, (velocity.magnitude/agent.Value.speed)*2f);

        // Return status based on distance to target
        float distanceToTarget = Vector3.Distance(ai.Value.transform.position, target.Value.transform.position);
        return distanceToTarget <= stoppingDistance.Value ? Status.Success : Status.Running;
    }

    protected override void OnEnd()
    {
        if (agent.Value)
            agent.Value.ResetPath();
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