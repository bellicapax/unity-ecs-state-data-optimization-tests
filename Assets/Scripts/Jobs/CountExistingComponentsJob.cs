using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct CountExistingComponentsJob : IJobParallelFor
{
    private int _count;

	public void Execute(int index)
	{
        _count++;
        //UnityEngine.Debug.Log("Count " + _count);
	}
}
