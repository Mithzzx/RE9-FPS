using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RandomIdle", story: "Set Random Idle animation", category: "Action", id: "f61ea039345661241ecfcba980ade9cd")]
public partial class RandomIdleAction : Action
{

    [SerializeReference] public BlackboardVariable<ZombieRandom> ai;

    protected override Status OnStart()
    {
        if (ai == null)
        {
            return Status.Failure;
        }
        ai.Value.SetIdleAnimation();
        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}

