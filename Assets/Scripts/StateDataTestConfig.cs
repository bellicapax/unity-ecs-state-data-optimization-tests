using UnityEngine;

[CreateAssetMenu(fileName ="Config", menuName ="Scriptable Objects/Config")]
public class StateDataTestConfig : ScriptableObject
{
    public TestMethod Method;
    [SerializeField] private uint _entityCount = 20000;
    public int EntityCount => (int)_entityCount;
    [SerializeField] private uint _changesPerFrame = 100;
    public int ChangesPerFrame => (int)_changesPerFrame;
    public byte StateCount = 1;
    public byte InterestingState = 0;
}
