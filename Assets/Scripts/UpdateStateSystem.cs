using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

[AlwaysUpdateSystem]
public class UpdateStateSystem : JobComponentSystem
{
    [Inject] private EndFrameBarrier _endFrameBarrier;
    [Inject] private WithComponentGroup _groupWithBinaryComponent;
    [Inject] private WithoutComponentGroup _groupWithoutBinaryComponent;

    private NativeArray<byte> _commands;
    private ComponentGroup _byteStateGroup;
    private StateDataTestConfig _config;
    private int _lastScheduledCommand;
    //private NativeArray<Entity> _spawnedEntities;

    protected override void OnCreateManager(int capacity)
    {
        _config = Resources.Load<StateDataTestConfig>("Config");
        SpawnInitialEntities();
        _commands = new NativeArray<byte>(_config.EntityCount, Allocator.Persistent);
        _byteStateGroup = GetComponentGroup(typeof(SharedByteState));

    }

    private void SpawnInitialEntities()
    {
        if (_config.Method == TestMethod.AddRemoveSharedComponent || _config.StateCount == 0)
            _config.StateCount = 1;

        if (_config.StateCount > 1)
        {
            var arch = EntityManager.CreateArchetype(typeof(SharedByteState));
            for (int i = 0; i < _config.EntityCount; i++)
            {
                EntityManager.CreateEntity(arch);
            }
        }
        else
        {
            for (int i = 0; i < _config.EntityCount; i++)
            {
                EntityManager.CreateEntity();
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var handles = new NativeList<JobHandle>(Allocator.Temp);
        handles.Add(ScheduleCounting(inputDeps));
        handles.Add(ScheduleCommandCreation(inputDeps));
        var combinedHandle = JobHandle.CombineDependencies(handles);
        handles.Dispose();
        return ScheduleStateUpdate(combinedHandle);
    }

    private JobHandle ScheduleCounting(JobHandle inputDeps)
    {
        switch (_config.Method)
        {
            //case TestMethod.InstancedComponent:
            //    break;

            case TestMethod.AddRemoveSharedComponent:
                return new CountExistingComponentsJob
                {
                }.Schedule(_groupWithBinaryComponent.Length, 64, inputDeps);

            case TestMethod.SetValueSharedComponentForEachFilter:
                var uniques = new List<SharedByteState>();
                EntityManager.GetAllUniqueSharedComponentDatas(uniques);
                var filter = _byteStateGroup.CreateForEachFilter<SharedByteState>(uniques);
                for (int i = 0; i < filter.Length; i++)
                {
                    if (uniques[i].State == _config.InterestingState)
                    {
                        var interestingCount = _byteStateGroup.GetEntityArray(filter, i).Length;
                        return new CountExistingComponentsJob
                        {
                        }.Schedule(interestingCount, 64, inputDeps);
                    }
                }
                filter.Dispose();
                return inputDeps;

            case TestMethod.SetValueSharedComponentSetFilter:
                _byteStateGroup.SetFilter(new SharedByteState { State = _config.InterestingState });
                var filteredLength = _byteStateGroup.CalculateLength();
                var handle = new CountExistingComponentsJob
                {
                }.Schedule(filteredLength, 64, inputDeps);
                _byteStateGroup.ResetFilter();
                return handle;

            case TestMethod.SetValueSharedComponentNoFilter:
                var length = _byteStateGroup.CalculateLength();
                if (length > 0)
                {
                    return new CountSpecificSharedStateJob
                    {
                        Datas = _byteStateGroup.GetSharedComponentDataArray<SharedByteState>(),
                        StateToCheckFor = _config.InterestingState

                    }.Schedule(length, 64, inputDeps);
                }
                else
                    return inputDeps;

            default:
                return inputDeps;
        }
    }

    private JobHandle ScheduleCommandCreation(JobHandle inputDeps)
    {
        switch (_config.Method)
        {
            //case TestMethod.InstancedComponent:
            //    break;
            case TestMethod.SetValueSharedComponentForEachFilter:
            case TestMethod.SetValueSharedComponentSetFilter:
            case TestMethod.SetValueSharedComponentNoFilter:
                return new CreateCommandsJob
                {
                    Commands = _commands,
                    StateCount = _config.StateCount
                }.Schedule(_config.ChangesPerFrame, 64, inputDeps);
            default:
                return inputDeps;
        }
    }

    private JobHandle ScheduleStateUpdate(JobHandle inputDeps)
    {
        switch (_config.Method)
        {
            //case TestMethod.InstancedComponent:
            //    break;
            case TestMethod.AddRemoveSharedComponent:
                var adds = _config.ChangesPerFrame - (_config.ChangesPerFrame / 2);
                var removes = _config.ChangesPerFrame - adds;
                var removeHandle = new RemoveBinaryStateComponentJob
                {
                    Entities = _groupWithBinaryComponent.Entities,
                    CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
                }.Schedule(removes, 64, inputDeps);
                var addHandle = new AddBinaryStateComponentJob
                {
                    Entities = _groupWithoutBinaryComponent.Entities,
                    CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
                }.Schedule(adds, 64, inputDeps);
                return JobHandle.CombineDependencies(removeHandle, addHandle);
            case TestMethod.SetValueSharedComponentForEachFilter:
            case TestMethod.SetValueSharedComponentSetFilter:
            case TestMethod.SetValueSharedComponentNoFilter:
                return new UpdateStateJob
                {
                    Entities = _byteStateGroup.GetEntityArray(),
                    Commands = _commands,
                    CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
                }.Schedule(_config.ChangesPerFrame, 64, inputDeps);
            default:
                return inputDeps;
        }
    }

}
