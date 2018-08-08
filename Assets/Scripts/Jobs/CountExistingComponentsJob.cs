using Unity.Jobs;

public struct CountExistingComponentsJob : IJobParallelFor
{
    private int _count;

	public void Execute(int index)
	{
        _count++;
	}
}
