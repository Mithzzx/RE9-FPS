using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.Serialization;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Random Animation", story: "Set [Ai] Random Animation", category: "Action", id: "e5dfe3ab980c2a748758e6ebfba528da")]
public partial class SetRandomAnimationAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ai;

    protected override Status OnStart()
    {
        ZombieRandom zombieRandom = ai.Value.GetComponent<ZombieRandom>();
        if (zombieRandom == null)
        {
            return Status.Failure;
        }
        zombieRandom.SetRandomAnimation();
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

