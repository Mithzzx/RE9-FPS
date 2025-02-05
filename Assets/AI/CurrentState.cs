using System;
using Unity.Behavior;

[BlackboardEnum]
public enum CurrentState
{
    Idel,
	Wander,
	Chase,
	Attack,
	Bite
}
