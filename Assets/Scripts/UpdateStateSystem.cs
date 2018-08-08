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
    [Inject] private WithInstancedBinaryComponentGroup _groupWithInstancedBinaryComponent;
    [Inject] private WithoutInstancedBinaryComponentGroup _groupWithoutInstancedBinaryComponent;

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

            case TestMethod.AddRemoveInstancedComponent:
                return new CountExistingComponentsJob
                {
                }.Schedule(_groupWithInstancedBinaryComponent.Length, 64, inputDeps);

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
                return inputDeps;

            case TestMethod.SetValueSharedComponentNoFilter:
                var length = _byteStateGroup.CalculateLength();
                if (length > 0)
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

            case TestMethod.AddRemoveInstancedComponent:
                var instancedAdds = Mathf.Min(_groupWithoutInstancedBinaryComponent.Length, _config.ChangesPerFrame - (_config.ChangesPerFrame / 2));
                var instancedRemoves = Mathf.Min(_groupWithInstancedBinaryComponent.Length, _config.ChangesPerFrame - instancedAdds);
                if (instancedAdds > 0 && instancedRemoves > 0)
                {
                    return JobHandle.CombineDependencies(
                        ScheduleAddingInstancedBinaryComponents(instancedAdds, inputDeps),
                        ScheduleRemovingInstancedBinaryComponents(instancedRemoves, inputDeps));
                }
                else if(instancedAdds > 0)
                {
                    return ScheduleAddingInstancedBinaryComponents(instancedAdds, inputDeps);
                }
                else if(instancedRemoves > 0)
                {
                    return ScheduleRemovingInstancedBinaryComponents(instancedRemoves, inputDeps);
                }
                return inputDeps;

            case TestMethod.AddRemoveSharedComponent:
                var sharedAdds = Mathf.Min(_groupWithoutSharedBinaryComponent.Length, _config.ChangesPerFrame - (_config.ChangesPerFrame / 2));
                var sharedRemoves = Mathf.Min(_groupWithSharedBinaryComponent.Length, _config.ChangesPerFrame - sharedAdds);
                if (sharedAdds > 0 && sharedRemoves > 0)
                {
                    return JobHandle.CombineDependencies(
                        ScheduleAddingSharedBinaryComponents(sharedAdds, inputDeps),
                        ScheduleRemovingSharedBinaryComponents(sharedRemoves, inputDeps));
                }
                else if (sharedAdds > 0)
                {
                    return ScheduleAddingSharedBinaryComponents(sharedAdds, inputDeps);
                }
                else if (sharedRemoves > 0)
                {
                    return ScheduleRemovingSharedBinaryComponents(sharedRemoves, inputDeps);
                }
                return inputDeps;

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

    private JobHandle ScheduleAddingInstancedBinaryComponents(int instancedAdds, JobHandle inputDeps)
    {
        return new AddInstancedBinaryStateComponentJob
        {
            Entities = _groupWithoutInstancedBinaryComponent.Entities,
            CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
        }.Schedule(instancedAdds, 64, inputDeps);
    }

    private JobHandle ScheduleRemovingInstancedBinaryComponents(int instancedRemoves, JobHandle inputDeps)
    {
        return new RemoveInstancedBinaryStateComponentJob
        {
            Entities = _groupWithInstancedBinaryComponent.Entities,
            CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
        }.Schedule(instancedRemoves, 64, inputDeps);
    }

    private JobHandle ScheduleAddingSharedBinaryComponents(int sharedAdds, JobHandle inputDeps)
    {
        return new AddSharedBinaryStateComponentJob
        {
            Entities = _groupWithoutSharedBinaryComponent.Entities,
            CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
        }.Schedule(sharedAdds, 64, inputDeps);
    }

    private JobHandle ScheduleRemovingSharedBinaryComponents(int sharedRemoves, JobHandle inputDeps)
    {
        return new RemoveSharedBinaryStateComponentJob
        {
            Entities = _groupWithSharedBinaryComponent.Entities,
            CommandBuffer = _endFrameBarrier.CreateCommandBuffer()
        }.Schedule(sharedRemoves, 64, inputDeps);
    }
    #endregion

    #region Initialization Helpers

    private void SpawnInitialEntities()
    {
        if (_config.Method == TestMethod.AddRemoveSharedComponent || _config.TotalStateCount == 0)
            _config.TotalStateCount = 1;

        if (_config.InterestingStateCount == 0)
            _config.InterestingStateCount = 1;

        if (_config.InterestingStateCount > _config.TotalStateCount)
            _config.TotalStateCount = _config.InterestingStateCount;

        switch (_config.Method)
        {
            case TestMethod.InstancedByteComponent:
                CreateEntitiesWithArchetype(EntityManager.CreateArchetype(typeof(InstancedByteState)));
                break;

            case TestMethod.AddRemoveInstancedComponent:
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
