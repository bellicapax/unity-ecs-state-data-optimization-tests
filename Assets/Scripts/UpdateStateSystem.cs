using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

[AlwaysUpdateSystem]
public class UpdateStateSystem : JobComponentSystem
{
    [Inject] private EndFrameBarrier _endFrameBarrier;
    [Inject] private WithSharedBinaryComponentGroup _groupWithSharedBinaryComponent;
    [Inject] private WithoutSharedBinaryComponentGroup _groupWithoutSharedBinaryComponent;
    [Inject] private WithInstancedByteComponentGroup _groupWithInstancedByteComponent;

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
            case TestMethod.InstancedByteComponent:
                if (_config.InterestingStateCount > 0)
                {
                    if (_config.InterestingStateCount == 1)
                    {
                        return new CountSpecificInstancedByteStateJob
                        {
                            Components = _groupWithInstancedByteComponent.Components,
                            StateToCheckFor = 0
                        }.Schedule(_groupWithInstancedByteComponent.Length, 64, inputDeps);
                    }
                    else
                    {
                        return new CountMultipleInstancedByteStatesJob
                        {
                            Components = _groupWithInstancedByteComponent.Components,
                            StatesToCheckFor = _statesToCheckFor
                        }.Schedule(_groupWithInstancedByteComponent.Length, 64, inputDeps);
                    }
                }
                return inputDeps;

            case TestMethod.AddRemoveSharedComponent:
                return new CountExistingComponentsJob
                {
                }.Schedule(_groupWithSharedBinaryComponent.Length, 64, inputDeps);

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
            case TestMethod.InstancedByteComponent:
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
            case TestMethod.InstancedByteComponent:
                return new UpdateInstancedByteStateJob
                {
                    Components = _groupWithInstancedByteComponent.Components,
                    States = _statesToWrite
                }.Schedule(_config.ChangesPerFrame, 64, inputDeps);

            case TestMethod.AddRemoveSharedComponent:
                var adds = _config.ChangesPerFrame - (_config.ChangesPerFrame / 2);
                var removes = _config.ChangesPerFrame - adds;
                var removeHandle = new RemoveSharedBinaryStateComponentJob
                {
                    Entities = _groupWithSharedBinaryComponent.Entities,
                    CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
                }.Schedule(removes, 64, inputDeps);
                var addHandle = new AddSharedBinaryStateComponentJob
                {
                    Entities = _groupWithoutSharedBinaryComponent.Entities,
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

        if (_config.InterestingStateCount == 0)
            _config.InterestingStateCount = 1;

        switch (_config.Method)
        {
            case TestMethod.InstancedByteComponent:
                CreateEntitiesWithArchetype(EntityManager.CreateArchetype(typeof(InstancedByteStateComponent)));
                break;

            case TestMethod.AddRemoveSharedComponent:
                for (int i = 0; i < _config.EntityCount; i++)
                {
                    EntityManager.CreateEntity();
                }
                break;

            case TestMethod.SetValueSharedComponentForEachFilter:
            case TestMethod.SetValueSharedComponentSetFilter:
            case TestMethod.SetValueSharedComponentNoFilter:
                CreateEntitiesWithArchetype(EntityManager.CreateArchetype(typeof(SharedByteState)));
                break;

            default:
                throw new System.Exception("No implementation to spawn entities for method " + _config.Method);
        }
    }

    private void CreateEntitiesWithArchetype(EntityArchetype arch)
    {
        for (int i = 0; i < _config.EntityCount; i++)
        {
            EntityManager.CreateEntity(arch);
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
