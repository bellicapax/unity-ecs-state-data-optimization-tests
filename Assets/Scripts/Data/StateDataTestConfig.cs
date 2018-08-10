using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName ="Config", menuName ="Scriptable Objects/Config")]
public class StateDataTestConfig : ScriptableObject
{
    public TestMethod Method;
    [SerializeField] private int _entityCount = 20000;
    public int EntityCount => _entityCount;
    [SerializeField] private RandomizableInt _changesPerFrame;
    public int ChangesPerFrame => _changesPerFrame.Value;
    [Header("States")]
    [SerializeField] private int _totalStateCount;
    public int TotalStateCount => _totalStateCount;
#warning I need to handle more states by checking if the value is above what a byte can hold and using different components & jobs
    [SerializeField] private RandomizableInt _interestingStateCount;
    public int InterestingStateCount => _interestingStateCount.Value;
}
