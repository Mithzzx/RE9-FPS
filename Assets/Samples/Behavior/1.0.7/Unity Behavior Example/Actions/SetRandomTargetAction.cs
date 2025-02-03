using System;
using Unity.Properties;
using UnityEngine;

namespace Unity.Behavior.Example
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Set Random Target",
        description: "Assigns a target to a random object matching a given tag.",
        story: "Set Random Target [Target] From Tag [TagValue]",
        id: "4daff47ae1c14ec780056d158e5e0953")]
    public partial class SetRandomTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<string> TagValue;

        protected override Status OnUpdate()
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(TagValue);
            if (tagged == null || tagged.Length == 0)
            {
                return Status.Failure;
            }

            int randomNumber = UnityEngine.Random.Range(0, tagged.Length);
            Target.Value = tagged[randomNumber];
            return Status.Success;
        }
    }
}