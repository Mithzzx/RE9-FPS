using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "NextAction", story: "Can Eat dead Zombie in [Range]", category: "Conditions", id: "1097021c6822a70662726386287d6e4d")]
public partial class NextActionCondition : Condition
{
    [FormerlySerializedAs("Range")] [SerializeReference] public BlackboardVariable<RangeDetector> range;
    [SerializeField] public BlackboardVariable<float> percentage = new BlackboardVariable<float>(30f); // Percentage chance to return true

    public override bool IsTrue()
    {
        if (!range.Value)
        {
            return false;
        }

        // Check if there is a dead zombie in range
        bool isDeadZombieInRange = range.Value.IsDeadZombieInHearingRange();

        // Return true based on the specified percentage
        if (isDeadZombieInRange)
        {
            Debug.Log("Can Eat dead Zombie in [Range]");
            return UnityEngine.Random.Range(0f, 100f) < percentage;
        }
        Debug.Log("Can't Eat dead Zombie in [Range]");
        return false;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}