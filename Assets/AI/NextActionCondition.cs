using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "NextAction", story: "Check if any dead Zombie in [sensor] and decide to eat", category: "Conditions")]
public partial class NextActionCondition : Condition
{
    [SerializeReference] public BlackboardVariable<AISensor> sensor;
    [SerializeField, Range(0, 100)] private float percentage = 100f;
    
    private static readonly System.Random Random = new System.Random();

    public override bool IsTrue()
    {
        if (!sensor?.Value)
        {
            Debug.LogError("Range detector is not set");
            return false;
        }

        GameObject[] filteredObjects = new GameObject[1];
        int count = sensor.Value.Filter(filteredObjects, "Dead", false);
        Debug.Log($"NextActionCondition: Found {count} dead zombies");

        return count > 0 && Random.NextDouble() * 100 < percentage;
    }
    
}