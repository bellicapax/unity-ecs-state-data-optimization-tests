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

    private NativeArray<byte> _statesToWrite;
    private NativeArray<byte> _statesToCheckFor;
    private ComponentGroup _byteStateGroup;
    private StateDataTestConfig _config;
    private int _lastScheduledCommand;

    protected override void OnCreateManager(int capacity)
    {
        _config = Resources.Load<StateDataTestConfig>("Config");
        SpawnInitialEntities();
        AllocateCommandsArray();
        FillStatesToCheckFor();
        _byteStateGroup = GetComponentGroup(typeof(SharedByteState));
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

    protected override void OnDestroyManager()
    {
        _statesToWrite.Dispose();
        _statesToCheckFor.Dispose();
    }

    #region Job Scheduling

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
                var filter = _byteStateGroup.CreateForEachFilter(uniques);
                for (int i = 0; i < filter.Length; i++)
                {
                    if (_config.InterestingStateCount > uniques[i].State)
                    {
                        var interestingCount = _byteStateGroup.GetEntityArray(filter, i).Length;
                        var forEachHandle = new CountExistingComponentsJob
                        {
                        }.Schedule(interestingCount, 64, inputDeps);
                        filter.Dispose();
                        return forEachHandle;
                    }
                }
                filter.Dispose();
                return inputDeps;

            case TestMethod.SetValueSharedComponentSetFilter:
                if (_config.InterestingStateCount > 0)
                {
                    if (_config.InterestingStateCount == 1)
                    {
                        var handle = ScheduleCountForFilterWithState(0, inputDeps);
                        _byteStateGroup.ResetFilter();
                        return handle;
                    }
                    else
                    {
                        var filterHandles = new NativeList<JobHandle>(_config.InterestingStateCount, Allocator.Temp);
                        for (int i = 0; i < _config.InterestingStateCount; i++)
                        {
                            filterHandles.Add(ScheduleCountForFilterWithState(i, inputDeps));
                        }
                        var combinedFilterHandles = JobHandle.CombineDependencies(filterHandles);
                        _byteStateGroup.ResetFilter();
                        filterHandles.Dispose();
                    }

                }
                return inputDeps;

            case TestMethod.SetValueSharedComponentNoFilter:
                var length = _byteStateGroup.CalculateLength();
                if (length > 0 && _config.InterestingStateCount > 0)
                {
                    if (_config.InterestingStateCount == 1)
                    {
                        return new CountSpecificSharedByteStateJob
                        {
                            Components = _byteStateGroup.GetSharedComponentDataArray<SharedByteState>(),
                            StateToCheckFor = 0
                        }.Schedule(length, 64, inputDeps);
                    }
                    else
                    {
                        return new CountMultipleSharedByteStatesJob
                        {
                            Components = _byteStateGroup.GetSharedComponentDataArray<SharedByteState>(),
                            StatesToCheckFor = _statesToCheckFor
                        }.Schedule(length, 64, inputDeps);
                    }
                }
                return inputDeps;

            default:
                return inputDeps;
        }
    }

    private JobHandle ScheduleCountForFilterWithState(int state, JobHandle inputDeps)
    {
        _byteStateGroup.SetFilter(new SharedByteState { State = (byte)state });
        var filteredLength = _byteStateGroup.CalculateLength();
        return new CountExistingComponentsJob
        {
        }.Schedule(filteredLength, 64, inputDeps);
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
                    Commands = _statesToWrite,
                    StateCount = _config.TotalStateCount
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
                var removeHandle = new RemoveSharedBinaryStateComponentJob
                {
                    Entities = _groupWithBinaryComponent.Entities,
                    CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
                }.Schedule(removes, 64, inputDeps);
                var addHandle = new AddSharedBinaryStateComponentJob
                {
                    Entities = _groupWithoutBinaryComponent.Entities,
                    CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
                }.Schedule(adds, 64, inputDeps);
                return JobHandle.CombineDependencies(removeHandle, addHandle);
            case TestMethod.SetValueSharedComponentForEachFilter:
            case TestMethod.SetValueSharedComponentSetFilter:
            case TestMethod.SetValueSharedComponentNoFilter:
                return new UpdateSharedByteStateJob
                {
                    Entities = _byteStateGroup.GetEntityArray(),
                    States = _statesToWrite,
                    CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
                }.Schedule(_config.ChangesPerFrame, 64, inputDeps);
            default:
                return inputDeps;
        }
    }

    #endregion

    #region Initialization Helpers

    private void SpawnInitialEntities()
    {
        if (_config.Method == TestMethod.AddRemoveSharedComponent || _config.TotalStateCount == 0)
            _config.TotalStateCount = 1;

        if (_config.TotalStateCount > 1)
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

    private void AllocateCommandsArray()
    {
        _statesToWrite = new NativeArray<byte>(_config.EntityCount, Allocator.Persistent);
    }

    private void FillStatesToCheckFor()
    {
        _statesToCheckFor = new NativeArray<byte>(_config.InterestingStateCount, Allocator.Persistent);
        for (byte i = 0; i < _config.InterestingStateCount; i++)
        {
            _statesToCheckFor[i] = i;
        }
    }

    #endregion
}
