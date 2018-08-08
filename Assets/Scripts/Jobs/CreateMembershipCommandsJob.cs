using Unity.Collections;
using Unity.Jobs;

public struct CreateMembershipCommandsJob : IJobParallelFor
{
    public NativeArray<byte> Commands;
    [ReadOnly] public int NumWithComponents;
    [ReadOnly] public int NumWithoutComponents;
    private int _counter;
    private int _adds;
    private int _removes;

    public void Execute(int index)
    {
        Commands[index] = (byte)MembershipCommand.None;
        if (_counter % 2 == 0 && _adds < NumWithoutComponents)
        {
            Commands[index] = (byte)MembershipCommand.Add;
            _adds++;
        }
        else if (_removes < NumWithComponents)
        {
            Commands[index] = (byte)MembershipCommand.Remove;
            _removes++;
        }
        _counter++;
    }
}

[System.Flags]
public enum MembershipCommand : byte
{
    None = 0,
    Add = 1,
    Remove = 2
}