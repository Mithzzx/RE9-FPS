using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Player in Sight", story: "[Player] is in sight of Zombie", category: "Conditions", id: "1ac3f648263f0d364ddb6365e5437785")]
public partial class PlayerInSightCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> player;
    [SerializeReference] public BlackboardVariable<AISensor> sensor;

    public override bool IsTrue()
    {
        GameObject[] filteredObjects = new GameObject[1];
        int count = sensor.Value.Filter(filteredObjects,"Player", true);
        return count > 0;
    }
}
