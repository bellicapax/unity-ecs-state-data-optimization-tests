using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct AddInstancedBinaryStateComponentJob : IJobParallelFor
{
    [ReadOnly] public EntityArray Entities;
    public EntityCommandBuffer.Concurrent CommandBuffer;

    public void Execute(int index)
    {
        CommandBuffer.AddComponent(Entities[index], new InstancedBinaryState { });
    }
}
