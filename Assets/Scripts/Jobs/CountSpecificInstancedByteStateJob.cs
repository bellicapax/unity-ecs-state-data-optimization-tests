using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct CountSpecificInstancedByteStateJob : IJobParallelFor
{
    [ReadOnly] public ComponentDataArray<InstancedByteStateComponent> Components;
    [ReadOnly] public byte StateToCheckFor;
    private int _count;

    public void Execute(int index)
    {
        if (Components[index].State == StateToCheckFor)
            _count++;
    }
}
