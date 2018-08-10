using UnityEngine;

[System.Serializable]
public class RandomizableInt : RandomizableData<int>
{
    protected override int RandomValue()
    {
        return Random.Range(_min, _max + 1);
    }

    public override void ConstrainDataInternally()
    {
        if(_min > _max)
        {
            if (_max != int.MinValue)
            {
                _min = _max - 1;
            }
            else
            {
                _min = _max;
                _max++;
            }
        }
    }

    public override void ConstrainDataExternally(int min, int max)
    {
        if (_min < min)
            _min = min;
        if (_max > max)
            _max = max;
        if (_constant < min)
            _constant = min;
        if (_constant > max)
            _constant = max;
        ConstrainDataInternally();
    }
}
