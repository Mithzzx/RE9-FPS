using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "NextAction", story: "Check if AI can eat dead zombie", category: "Conditions")]
public partial class NextActionCondition : Condition
{
    [SerializeReference] public BlackboardVariable<RangeDetector> range;
    [SerializeField, Range(0, 100)] private float percentage = 30f;
    
    private static readonly System.Random Random = new System.Random();

    public override bool IsTrue()
    {
        if (range?.Value == null) return false;
        
        return range.Value.IsDeadZombieInRange() && 
               Random.NextDouble() * 100 < percentage;
    }
}