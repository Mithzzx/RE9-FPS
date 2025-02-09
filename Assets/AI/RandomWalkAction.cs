using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RandomWalk", story: "Set a Random Walk animation", category: "Action", id: "568bc25c573f7db7dc0a0e4f4e640a3e")]
public partial class RandomWalkAction : Action
{
    [SerializeReference] public BlackboardVariable<ZombieRandom> ai;

    protected override Status OnStart()
    {
        if (ai == null)
        {
            return Status.Failure;
        }
        ai.Value.SetWalkAnimation();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

