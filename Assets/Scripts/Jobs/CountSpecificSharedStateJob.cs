using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct CountSpecificSharedStateJob : IJobParallelFor
{
    [ReadOnly] public SharedComponentDataArray<SharedByteState> Datas;
    [ReadOnly] public byte StateToCheckFor;
    private int _count;

    public void Execute(int index)
	{
        if (Datas[index].State == StateToCheckFor)
            _count++;
    }
}
