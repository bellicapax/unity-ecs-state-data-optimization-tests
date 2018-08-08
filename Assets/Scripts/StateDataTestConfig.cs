using UnityEngine;

[CreateAssetMenu(fileName ="Config", menuName ="Scriptable Objects/Config")]
public class StateDataTestConfig : ScriptableObject
{
    public TestMethod Method;
    [SerializeField] private uint _entityCount = 20000;
    public int EntityCount => (int)_entityCount;
    [SerializeField] private uint _changesPerFrame = 100;
    public int ChangesPerFrame => (int)_changesPerFrame;
#warning I need to handle more states by checking if the value is above what a byte can hold and using different components & jobs
    public byte TotalStateCount = 1;
    public byte InterestingStateCount;
}
