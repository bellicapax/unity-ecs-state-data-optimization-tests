using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct UpdateStateJob : IJobParallelFor
{
    [ReadOnly] public EntityArray Entities;
    [ReadOnly] public NativeArray<byte> Commands;
    public EntityCommandBuffer.Concurrent CommandBuffer;

    public void Execute(int index)
    {
        CommandBuffer.SetSharedComponent(Entities[index], new SharedByteState { State = Commands[index] });
    }
}
