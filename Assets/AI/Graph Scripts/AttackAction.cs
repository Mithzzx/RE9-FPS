using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Attack", story: "[Ai] Attacks [Target]", category: "Action", id: "51cfd4c907ffb33ce0ef32936fc4a83f")]
public partial class AttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Ai;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

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

