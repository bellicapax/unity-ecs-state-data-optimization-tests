using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct RemoveSharedBinaryStateComponentJob : IJobParallelFor
{
    [ReadOnly] public EntityArray Entities;
    public EntityCommandBuffer.Concurrent CommandBuffer;

    public void Execute(int index)
    {
        CommandBuffer.RemoveComponent<SharedBinaryState>(Entities[index]);
    }
}
