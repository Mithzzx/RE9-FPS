using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Attack Deduct", story: "Check if [Target] in [AttackRange]", category: "Action", id: "4d3de7f16a5a073b5ce8727aa079a52f")]
public partial class AttackDeductAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<RangeDetector> AttackRange;

    protected override Status OnStart()
    {
        if (AttackRange.Value.IsTargetInAttackRange())
        {
            return Status.Success;
        }
        else
        {
            return Status.Failure;
        }
    }
}

