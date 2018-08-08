using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct UpdateInstancedByteStateJob : IJobParallelFor
{
    [WriteOnly] [NativeDisableParallelForRestriction] public ComponentDataArray<InstancedByteState> Components;
    [ReadOnly] public NativeArray<byte> States;

    public void Execute(int index)
	{
        Components[index] = new InstancedByteState { State = States[index] };
	}
}
