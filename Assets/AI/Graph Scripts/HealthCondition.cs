using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Health", story: "Check if [AI] has [Health]", category: "Conditions", id: "420d82c04f6011424cdc6c4620cda3bd")]
public partial class HealthCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> AI;
    [SerializeReference] public BlackboardVariable<Health> Health;

    public override bool IsTrue()
    {
        return Health.Value.IsDead;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
