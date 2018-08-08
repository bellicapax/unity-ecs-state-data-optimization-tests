using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct CountMultipleInstancedByteStatesJob : IJobParallelFor
{
    [ReadOnly] public ComponentDataArray<InstancedByteStateComponent> Components;
    [ReadOnly] public NativeArray<byte> StatesToCheckFor;
    private int _count;

    public void Execute(int index)
    {
        if (StatesToCheckFor.Contains(Components[index].State))
            _count++;
    }
}
