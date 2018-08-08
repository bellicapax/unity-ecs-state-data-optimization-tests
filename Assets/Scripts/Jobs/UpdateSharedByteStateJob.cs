using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct UpdateSharedByteStateJob : IJobParallelFor
{
    [ReadOnly] public EntityArray Entities;
    [ReadOnly] public NativeArray<byte> States;
    public EntityCommandBuffer.Concurrent CommandBuffer;

    public void Execute(int index)
    {
        CommandBuffer.SetSharedComponent(Entities[index], new SharedByteState { State = States[index] });
    }
}
