using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct AddBinaryStateComponentJob : IJobParallelFor
{
    [ReadOnly] public EntityArray Entities;
    public EntityCommandBuffer.Concurrent CommandBuffer;

    public void Execute(int index)
    {
        if (index < Entities.Length)
            CommandBuffer.AddSharedComponent(Entities[index], new SharedBinaryState { });
    }
}
