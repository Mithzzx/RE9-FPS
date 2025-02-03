using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Idel Action", story: "Agent is [idel]", category: "Action", id: "95c61863b11fca9c8d2ec0ced613f6b6")]
public partial class IdelAction : Action
{
    [SerializeReference] public BlackboardVariable<bool> Idel;

    protected override Status OnStart()
    {
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

