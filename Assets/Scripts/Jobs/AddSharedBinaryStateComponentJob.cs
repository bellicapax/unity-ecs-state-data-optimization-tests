﻿using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct AddSharedBinaryStateComponentJob : IJobParallelFor
{
    [ReadOnly] public EntityArray Entities;
    public EntityCommandBuffer.Concurrent CommandBuffer;

    public void Execute(int index)
    {
        CommandBuffer.AddSharedComponent(Entities[index], new SharedBinaryState { });
    }
}
