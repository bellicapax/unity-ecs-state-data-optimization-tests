using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct CountMultipleSharedByteStatesJob : IJobParallelFor
{
    [ReadOnly] public SharedComponentDataArray<SharedByteState> Components;
    [ReadOnly] public NativeArray<byte> StatesToCheckFor;
    private int _count;

    public void Execute(int index)
    {
        if (StatesToCheckFor.Contains(Components[index].State))
            _count++;
    }
}
