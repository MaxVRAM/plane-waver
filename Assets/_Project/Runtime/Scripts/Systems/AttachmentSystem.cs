using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System;

using MaxVRAM;
using PlaneWaver;
using PlaneWaver.Emitters;

// https://docs.unity3d.com/Packages/com.unity.entities@0.51/api/

/// <summary>
///  Processes dynamic emitter frame speaker link components amd updates entity in-range statuses.
/// </summary>
[UpdateAfter(typeof(DOTS_QuadrantSystem))]
public partial class AttachmentSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _commandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var connectConfig = GetSingleton<ConnectionComponent>();
        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter ecb = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();


        EntityQueryDesc frameQueryDesc = new()
        {
            All = new ComponentType[] { typeof(FrameComponent)}
        };

        EntityQueryDesc frameConnectedQueryDesc = new()
        {
            All = new ComponentType[] { typeof(FrameComponent), typeof(SpeakerConnection), typeof(InListenerRangeTag) }
        };

        EntityQueryDesc frameAloneQueryDesc = new()
        {
            All = new ComponentType[] { typeof(FrameComponent), typeof(SpeakerConnection),
                typeof(AloneOnSpeakerTag), typeof(InListenerRangeTag) }
        };


        EntityQueryDesc frameNotAloneQueryDesc = new()
        {
            All = new ComponentType[] { typeof(FrameComponent), typeof(SpeakerConnection), typeof(InListenerRangeTag) },
            None = new ComponentType[] { typeof(AloneOnSpeakerTag) }
        };

        EntityQueryDesc frameDisconnectedQueryDesc = new()
        {
            All = new ComponentType[] { typeof(FrameComponent), typeof(InListenerRangeTag) },
            None = new ComponentType[] { typeof(SpeakerConnection)}
        };
        EntityQueryDesc speakerQueryDesc = new()
        {
            All = new ComponentType[] { typeof(SpeakerIndex), typeof(SpeakerComponent) }
        };

        EntityQuery speakerQuery;
        NativeArray<Entity> speakerEntities;
        NativeArray<SpeakerIndex> speakerIndexes;
        NativeArray<SpeakerComponent> speakerComponents;
        
        EntityQuery frameQuery;
        NativeArray<Entity> frameEntities;
        NativeArray<FrameComponent> frameComponents;
        NativeArray<SpeakerConnection> frameConnections;


        speakerQuery = GetEntityQuery(speakerQueryDesc);
        speakerIndexes = speakerQuery.ToComponentDataArray<SpeakerIndex>(Allocator.TempJob);
        speakerComponents = speakerQuery.ToComponentDataArray<SpeakerComponent>(Allocator.TempJob);
        JobHandle frameToSpeakerCheckJob = Entities.WithName("FrameToSpeakerCheck")
            .WithAll<InListenerRangeTag>()
            .WithReadOnly(speakerIndexes).WithReadOnly(speakerComponents).ForEach
        (
            (
                int entityInQueryIndex, Entity entity, ref SpeakerConnection connection, in FrameComponent frame) =>
            {
                if (connection.SpeakerIndex < 0 || connection.SpeakerIndex >= speakerComponents.Length)
                {
                    ecb.RemoveComponent<SpeakerConnection>(entityInQueryIndex, entity);
                    ecb.RemoveComponent<AloneOnSpeakerTag>(entityInQueryIndex, entity);
                    return;
                }
                
                foreach (SpeakerIndex speakerIndex in speakerIndexes)
                {
                    if (speakerIndex.Value != connection.SpeakerIndex)
                        continue;

                    SpeakerComponent speakerComponent = speakerComponents[speakerIndex.Value];

                    if (math.distance(frame.Position, speakerComponent.Position) <= speakerComponent.Radius)
                    {
                        if (speakerComponents[speakerIndex.Value].ConnectedHostCount == 1)
                            ecb.AddComponent(entityInQueryIndex, entity, new AloneOnSpeakerTag());
                        return;
                    }

                    break;
                }

                ecb.RemoveComponent<SpeakerConnection>(entityInQueryIndex, entity);
                ecb.RemoveComponent<AloneOnSpeakerTag>(entityInQueryIndex, entity);
            }
        ).ScheduleParallel(Dependency);
        frameToSpeakerCheckJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(frameToSpeakerCheckJob);


        //speakerQuery = GetEntityQuery(speakerQueryDesc);
        JobHandle groupLoneFramesJob = Entities.WithName("GroupLoneFrames")
            .WithAll<InListenerRangeTag, AloneOnSpeakerTag>()
            .WithReadOnly(speakerIndexes).WithReadOnly(speakerComponents).ForEach
        (
            (int entityInQueryIndex, Entity entity, ref SpeakerConnection connection, in FrameComponent frame) =>
            {
                var foundSelf = false;
                var foundOther = false;
                int otherSpeakerIndex = -1;
                float selfInactive = 0;
                float otherInactive = 0;
                
                for (var s = 0; s < speakerComponents.Length; s++)
                {
                    if (speakerComponents[s].State != ConnectionState.Active)
                        continue;

                    if (speakerIndexes[s].Value == connection.SpeakerIndex)
                    {
                        selfInactive = speakerComponents[s].InactiveDuration;
                        foundSelf = true;
                        if (foundOther)
                            break;

                        continue;
                    }
                    
                    if (speakerComponents[s].Radius < math.distance(frame.Position, speakerComponents[s].Position))
                        continue;

                    otherSpeakerIndex = speakerIndexes[s].Value;
                    otherInactive = speakerComponents[s].InactiveDuration;
                    foundOther = true;
                    if (foundSelf)
                        break;
                }

                if (!foundSelf || !foundOther || selfInactive < otherInactive)
                    return;

                connection.SpeakerIndex = otherSpeakerIndex;
                ecb.RemoveComponent<AloneOnSpeakerTag>(entityInQueryIndex, entity);
            }
        ).ScheduleParallel(frameToSpeakerCheckJob);
        groupLoneFramesJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(groupLoneFramesJob);

        
        JobHandle connectToActiveSpeakerJob = Entities.WithName("ConnectToActiveSpeaker")
            .WithAll<InListenerRangeTag>().WithNone<SpeakerConnection>()
            .WithReadOnly(speakerIndexes).WithReadOnly(speakerComponents).ForEach
        (
            (int entityInQueryIndex, Entity entity, ref FrameComponent frame) =>
            {
                var foundOther = false;
                int otherSpeakerIndex = -1;

                for (var s = 0; s < speakerComponents.Length; s++)
                {
                    SpeakerComponent speaker = speakerComponents[s];

                    if (speaker.State == ConnectionState.Pooled ||
                        speaker.GrainLoad > connectConfig.BusyLoadLimit ||
                        speaker.Radius < math.distance(frame.Position, speaker.Position))
                        continue;
                    
                    otherSpeakerIndex = speakerIndexes[s].Value;
                    foundOther = true;
                    
                    if (speaker.State == ConnectionState.Lingering) { break; }
                }

                if (!foundOther)
                    return;

                ecb.AddComponent(entityInQueryIndex, entity, new SpeakerConnection
                {
                    SpeakerIndex = otherSpeakerIndex
                });
            }
        ).WithDisposeOnCompletion(speakerIndexes).WithDisposeOnCompletion(speakerComponents)
        .ScheduleParallel(groupLoneFramesJob);
        connectToActiveSpeakerJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(connectToActiveSpeakerJob);

        
        frameQuery = GetEntityQuery(frameConnectedQueryDesc);
        frameComponents = frameQuery.ToComponentDataArray<FrameComponent>(Allocator.TempJob);
        frameConnections = frameQuery.ToComponentDataArray<SpeakerConnection>(Allocator.TempJob);
        JobHandle moveSpeakersJob = Entities.WithName("MoveSpeakers")
            .WithReadOnly(frameComponents).WithReadOnly(frameConnections).ForEach
        (
            (ref SpeakerComponent speaker, in SpeakerIndex index) =>
            {
                var attachedFrames = 0;
                float3 currentPosition = speaker.Position;
                float3 targetPosition = new(0, 0, 0);
                
                for (var f = 0; f < frameComponents.Length; f++)
                {
                    if (frameConnections[f].SpeakerIndex != index.Value
                            || math.distance(currentPosition, frameComponents[f].Position) > speaker.Radius)
                        continue;

                    targetPosition += frameComponents[f].Position;
                    attachedFrames++;
                }

                speaker.ConnectedHostCount = attachedFrames;

                if (attachedFrames == 0)
                {
                    speaker.InactiveDuration += connectConfig.DeltaTime;
                    switch (speaker.State)
                    {
                        case ConnectionState.Lingering when speaker.InactiveDuration >= connectConfig.SpeakerLingerTime:
                            speaker.State = ConnectionState.Pooled;
                            speaker.Radius = 0.001f;
                            speaker.Position = connectConfig.DisconnectedPosition;
                            break;

                        case ConnectionState.Active:
                            speaker.State = ConnectionState.Lingering;
                            speaker.InactiveDuration = 0;
                            speaker.Position = currentPosition;
                            speaker.Radius = CalculateSpeakerRadius(
                                connectConfig.ListenerPos, currentPosition, connectConfig.ArcDegrees);
                            break;
                    }
                    return;
                }

                if (speaker.State != ConnectionState.Active)
                {
                    speaker.InactiveDuration = 0;
                    speaker.State = ConnectionState.Active;
                }

                speaker.InactiveDuration -= connectConfig.DeltaTime;

                targetPosition = attachedFrames == 1 ? targetPosition : targetPosition / attachedFrames;
                float3 lerpPosition = math.lerp(currentPosition, targetPosition, connectConfig.TranslationSmoothing);
                float lerpRadius = CalculateSpeakerRadius(
                    connectConfig.ListenerPos,lerpPosition, connectConfig.ArcDegrees);

                if (lerpRadius > math.distance(lerpPosition, targetPosition))
                {
                    speaker.Position = lerpPosition;
                    speaker.Radius = lerpRadius;
                }
                else
                {
                    speaker.Position = targetPosition;
                    speaker.Radius = CalculateSpeakerRadius(
                        connectConfig.ListenerPos, targetPosition, connectConfig.ArcDegrees);
                }
            }
        ).WithDisposeOnCompletion(frameComponents).WithDisposeOnCompletion(frameConnections)
        .ScheduleParallel(connectToActiveSpeakerJob);
        moveSpeakersJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(moveSpeakersJob);

        
        frameQuery = GetEntityQuery(frameDisconnectedQueryDesc);
        frameEntities = frameQuery.ToEntityArray(Allocator.TempJob);
        frameComponents = frameQuery.ToComponentDataArray<FrameComponent>(Allocator.TempJob);
        speakerEntities = speakerQuery.ToEntityArray(Allocator.TempJob);
        speakerIndexes = speakerQuery.ToComponentDataArray<SpeakerIndex>(Allocator.TempJob);
        speakerComponents = speakerQuery.ToComponentDataArray<SpeakerComponent>(Allocator.TempJob);      
        
        JobHandle speakerActivationJob = Job.WithName("speakerActivation")
            .WithReadOnly(speakerIndexes).WithoutBurst().WithCode(() =>
        {
            for (var f = 0; f < frameEntities.Length; f++)
            {
                for (var s = 0; s < speakerComponents.Length; s++)
                {
                    if (speakerComponents[s].State != ConnectionState.Pooled)
                        continue;
        
                    ecb.AddComponent(f, frameEntities, new SpeakerConnection
                    {
                        SpeakerIndex = speakerIndexes[s].Value
                    });
                    ecb.AddComponent(f, frameEntities, new AloneOnSpeakerTag());
        
                    float radius = CalculateSpeakerRadius(
                        connectConfig.ListenerPos, frameComponents[f].Position, connectConfig.ArcDegrees);
                    
                    ecb.SetComponent(s, speakerEntities[s], new SpeakerComponent
                    {
                        State = ConnectionState.Active,
                        ConnectedHostCount = 1,
                        Radius = radius,
                        InactiveDuration = 0,
                        GrainLoad = 0,
                        Position = frameComponents[f].Position
                    });
                    
                    return;
                }
            }
        }).WithDisposeOnCompletion(frameEntities).WithDisposeOnCompletion(frameComponents)
        .WithDisposeOnCompletion(speakerEntities).WithDisposeOnCompletion(speakerComponents)
        .WithDisposeOnCompletion(speakerIndexes).Schedule(moveSpeakersJob);
        speakerActivationJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(speakerActivationJob);

        Dependency = moveSpeakersJob;
    }

    private static float CalculateSpeakerRadius(float3 listenerPos, float3 speakerPos, float arcLength)
    {
        var listenerCircumference = (float)(2 * Math.PI * math.distance(listenerPos, speakerPos));
        return arcLength * MaxMath.ONE_DEGREE * listenerCircumference;
    }
}
