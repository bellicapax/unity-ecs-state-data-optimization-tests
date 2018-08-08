using Unity.Collections;
using Unity.Entities;

public struct WithSharedBinaryComponentGroup
{
    [ReadOnly] public SharedComponentDataArray<SharedBinaryState> InterestingComponents;
    [ReadOnly] public EntityArray Entities;
    public readonly int Length;
}

public struct WithoutSharedBinaryComponentGroup
{
    [ReadOnly] public SubtractiveComponent<SharedBinaryState> NoBinaryState;
    [ReadOnly] public EntityArray Entities;
    public readonly int Length;
}

public struct WithInstancedByteComponentGroup
{
    public ComponentDataArray<InstancedByteStateComponent> Components;
    public readonly int Length;
}