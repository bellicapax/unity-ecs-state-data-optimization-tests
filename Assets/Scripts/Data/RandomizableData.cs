using UnityEngine;

[System.Serializable]
public abstract class RandomizableData<T>
{
    public bool UseRandom;
    [SerializeField] protected T _min;
    public T Min => _min;
    [SerializeField] protected T _max;
    public T Max => _max;
    [SerializeField] protected T _constant;
    public T Constant => _constant;

    public T Value => UseRandom ? RandomValue() : _constant;
    protected abstract T RandomValue();
    public abstract void ConstrainDataInternally();
    public abstract void ConstrainDataExternally(T min, T max);
}
