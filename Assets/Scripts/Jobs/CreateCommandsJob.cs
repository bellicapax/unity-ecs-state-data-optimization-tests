using Unity.Collections;
using Unity.Jobs;

public struct CreateCommandsJob : IJobParallelFor
{
    public NativeArray<byte> Commands;
    public byte StateCount;
    public byte Offset;

	public void Execute(int index)
	{
        Commands[index] = (byte)((index + Offset) % StateCount);
    }
}
