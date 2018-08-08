using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public struct RemoveInstancedBinaryStateComponentJob : IJobParallelFor
{
    [ReadOnly] public EntityArray Entities;
    public EntityCommandBuffer.Concurrent CommandBuffer;

    public void Execute(int index)
    {
        CommandBuffer.RemoveComponent<InstancedBinaryState>(Entities[index]);
    }
}
