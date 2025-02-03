using System;
using Unity.Properties;
using UnityEngine;

namespace Unity.Behavior.SerializationExample
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ChooseTargetPosition", story: "Choose [TargetPosition]", category: "Action", id: "7ff79b93c9fb36dc46b238e19efaa31b")]
    public partial class ChooseTargetPosition : Action
    {
        [SerializeReference] public BlackboardVariable<Vector3> TargetPosition;
        [SerializeReference] public BlackboardVariable<Vector3> Min;
        [SerializeReference] public BlackboardVariable<Vector3> Max;

        protected override Status OnStart()
        {
            Vector3 position = new Vector3(
                UnityEngine.Random.Range(Min.Value.x, Max.Value.x),
                UnityEngine.Random.Range(Min.Value.y, Max.Value.y),
                UnityEngine.Random.Range(Min.Value.z, Max.Value.z)
            );
            TargetPosition.Value = position;
            return Status.Success;
        }
    }
}
