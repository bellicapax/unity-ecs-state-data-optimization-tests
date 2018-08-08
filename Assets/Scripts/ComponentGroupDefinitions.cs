using Unity.Collections;
using Unity.Entities;

public struct WithComponentGroup
{
    [ReadOnly] public SharedComponentDataArray<SharedBinaryState> InterestingComponents;
    [ReadOnly] public EntityArray Entities;
    public readonly int Length;
}

public struct WithoutComponentGroup
{
    [ReadOnly] public SubtractiveComponent<SharedBinaryState> NoBinaryState;
    [ReadOnly] public EntityArray Entities;
    public readonly int Length;
}