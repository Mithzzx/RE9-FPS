using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Serialization;
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "NextAction", story: "Check for dead zombies in range and decide to Eat", 
    category: "Conditions", id: "1097021c6822a70662726386287d6e4d")]
public partial class NextActionCondition : Condition
{
    [SerializeReference] private BlackboardVariable<RangeDetector> range;
    [SerializeField] private BlackboardVariable<float> percentage = new(100f);

    private static readonly System.Random Random = new();

    public override bool IsTrue()
    {
        var detector = range.Value;
        if (detector == null) return false;
        
        if (detector.IsDeadZombieInHearingRange()) 
        {Debug.Log("Dead zombie is in hearing range");}
        else
        {
            Debug.Log("Dead zombie is not in hearing range");
        }

        return detector.IsDeadZombieInHearingRange() && 
               Random.NextDouble() * 100 < percentage.Value;
    }

    public override void OnStart()
    {
        if (range.Value == null) Debug.Log("RangeDetector is not set");
    }
    public override void OnEnd() { }
}
