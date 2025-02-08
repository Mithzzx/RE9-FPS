using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "AttackRange", story: "Check if [Target] in [AttackRang]", category: "Conditions", id: "3fc36c3be4b443e255f1121d7e7c4e3b")]
public partial class AttackRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> target;
    [SerializeReference] public BlackboardVariable<RangeDetector> attackRang;

    public override bool IsTrue()
    {
        return attackRang.Value.IsTargetInAttackRange();
    }

    public override void OnStart()
    {
        // Initialization logic if needed
    }
}