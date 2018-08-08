using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct CountSpecificSharedByteStateJob : IJobParallelFor
{
    [ReadOnly] public SharedComponentDataArray<SharedByteState> Components;
    [ReadOnly] public byte StateToCheckFor;
    private int _count;

    public void Execute(int index)
	{
        if (Components[index].State == StateToCheckFor)
            _count++;
    }
}
