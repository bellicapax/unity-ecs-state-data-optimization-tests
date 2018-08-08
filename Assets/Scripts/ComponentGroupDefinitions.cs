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
    public ComponentDataArray<InstancedByteState> Components;
    public readonly int Length;
}

public struct WithInstancedBinaryComponentGroup
{
    [ReadOnly] public ComponentDataArray<InstancedBinaryState> InterestingComponents;
    [ReadOnly] public EntityArray Entities;
    public readonly int Length;
}

public struct WithoutInstancedBinaryComponentGroup
{
    [ReadOnly] public SubtractiveComponent<InstancedBinaryState> NoBinaryState;
    [ReadOnly] public EntityArray Entities;
    public readonly int Length;
}