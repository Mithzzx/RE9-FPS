using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Deduct", story: "Deducts if [Target] is in range", category: "Action", id: "effcba5b1d3e06118167318b60236ff0")]
public partial class DeductAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Ai;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<bool> IsInRange; 
    [SerializeField] public BlackboardVariable<float> range = new BlackboardVariable<float>(5f);

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Ai.Value == null || Target.Value == null)
        {
            return Status.Failure;
        }

        float distanceToTarget = Vector3.Distance(Ai.Value.transform.position, Target.Value.transform.position);
        IsInRange.Value = distanceToTarget <= range;

        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}