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
        var dspTimer = GetSingleton<AudioTimerComponent>();
        var connectConfig = GetSingleton<ConnectionConfig>();
        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter ecb = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();


        EntityQueryDesc hostQueryDesc = new()
        {
            All = new ComponentType[] { typeof(FrameComponent)}
        };

        EntityQueryDesc frameConnectedQueryDesc = new()
        {
            All = new ComponentType[] { typeof(FrameComponent), typeof(SpeakerIndex) }
        };

        EntityQueryDesc frameAloneQueryDesc = new()
        {
            All = new ComponentType[] { typeof(FrameComponent), typeof(SpeakerIndex), typeof(AloneOnSpeakerTag) }
        };


        EntityQueryDesc frameNotAloneQueryDesc = new()
        {
            All = new ComponentType[] { typeof(FrameComponent), typeof(SpeakerIndex) },
            None = new ComponentType[] { typeof(AloneOnSpeakerTag) }
        };

        EntityQueryDesc frameDisconnectedQueryDesc = new()
        {
            All = new ComponentType[] { typeof(FrameComponent), typeof(InListenerRadiusTag) },
            None = new ComponentType[] { typeof(SpeakerIndex)}
        };
        EntityQueryDesc speakerQueryDesc = new()
        {
            All = new ComponentType[] { typeof(SpeakerIndex), typeof(SpeakerComponent) }
        };

        EntityQuery hostQuery;
        EntityQuery speakerQuery;

        NativeArray<Entity> frameEntities;
        NativeArray<Entity> speakerEntities;
        NativeArray<SpeakerIndex> speakerIndexes;
        NativeArray<FrameComponent> frameComponents;
        NativeArray<SpeakerComponent> speakerComponents;

        speakerQuery = GetEntityQuery(speakerQueryDesc);
        speakerIndexes = speakerQuery.ToComponentDataArray<SpeakerIndex>(Allocator.TempJob);
        speakerComponents = speakerQuery.ToComponentDataArray<SpeakerComponent>(Allocator.TempJob);
        JobHandle updateFrameRangeJob = Entities.WithName("UpdateFrameRange").WithReadOnly(speakerComponents).ForEach
        (
            (int entityInQueryIndex, Entity entity, ref FrameComponent frame) =>
            {
                if (math.distance(frame.Position, connectConfig.ListenerPos) > connectConfig.ListenerRadius)
                {
                    ecb.RemoveComponent<SpeakerIndex>(entityInQueryIndex, entity);
                    ecb.RemoveComponent<InListenerRangeTag>(entityInQueryIndex, entity);
                    ecb.RemoveComponent<AloneOnSpeakerTag>(entityInQueryIndex, entity);
                    return;
                }

                ecb.AddComponent(entityInQueryIndex, entity, new InListenerRangeTag());
                GetComponentDataFromEntity<HostComponent>(true);
                var speakerIndex = GetComponent<SpeakerIndex>(entity);
                if (speakerIndex.Value < speakerComponents.Length)
                {
                    for (var s = 0;  s < speakerComponents.Length; s++)
                    {
                        if (speakerIndex.Value != s)
                            continue;

                        SpeakerComponent speaker = speakerComponents[s];

                        if (math.distance(frame.Position, speaker.WorldPos) <= speaker.ConnectionRadius)
                        {
                            if (speakerComponents[speakerIndex.Value].ConnectedHostCount == 1)
                                ecb.AddComponent(entityInQueryIndex, entity, new AloneOnSpeakerTag());
                            return;
                        }

                        break;
                    }
                }

                host.Connected = false;
                host.SpeakerIndex = int.MaxValue;
                ecb.RemoveComponent<ConnectedTag>(entityInQueryIndex, entity);
                ecb.RemoveComponent<LoneHostOnSpeakerTag>(entityInQueryIndex, entity);
            }
        ).ScheduleParallel(Dependency);
        updateFrameRangeJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(updateFrameRangeJob);

        speakerQuery = GetEntityQuery(speakerQueryDesc);
        JobHandle groupLoneHostsJob = Entities.WithName("GroupLoneHosts")
            .WithAll<InListenerRadiusTag, ConnectedTag, LoneHostOnSpeakerTag>()
            .WithReadOnly(speakerIndexes).WithReadOnly(speakerComponents).ForEach
        (
            (int entityInQueryIndex, Entity entity, ref HostComponent host) =>
            {
                if (host.SpeakerIndex >= speakerComponents.Length)
                    return;

                int otherIndex = int.MaxValue;
                float currentInactiveDuration = float.MaxValue;
                float otherSpeakerInactiveDuration = float.MaxValue;

                for (int s = 0; s < speakerComponents.Length; s++)
                {
                    if (speakerComponents[s].State != ConnectionState.Active)
                        continue;

                    if (speakerIndexes[s].Value == host.SpeakerIndex)
                    {
                        currentInactiveDuration = speakerComponents[s].InactiveDuration;
                        if (otherSpeakerInactiveDuration < 0)
                            break;
                    }
                    else if (speakerComponents[s].ConnectedHostCount > 0 && 
                        math.distance(host.WorldPos, speakerComponents[s].WorldPos) < speakerComponents[s].ConnectionRadius)
                    {
                        otherIndex = speakerIndexes[s].Value;
                        otherSpeakerInactiveDuration = speakerComponents[s].InactiveDuration;
                        if (currentInactiveDuration < 0)
                            break;
                    }
                }

                if (currentInactiveDuration > 0 || otherIndex > speakerComponents.Length)
                    return;

                if (otherSpeakerInactiveDuration < currentInactiveDuration)
                {
                    host.SpeakerIndex = otherIndex;
                    ecb.RemoveComponent<LoneHostOnSpeakerTag>(entityInQueryIndex, entity);
                }
            }
        ).ScheduleParallel(updateFrameRangeJob);
        groupLoneHostsJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(groupLoneHostsJob);

        JobHandle connectToActiveSpeakerJob = Entities.WithName("ConnectToActiveSpeaker").
            WithNone<ConnectedTag>().WithAll<InListenerRadiusTag>().WithReadOnly(speakerIndexes)
            .WithReadOnly(speakerComponents).ForEach
        (
            (int entityInQueryIndex, Entity entity, ref HostComponent host) =>
            {
                int newSpeakerIndex = int.MaxValue;
                float lowestActiveGrainLoad = 1;

                for (int i = 0; i < speakerComponents.Length; i++)
                {
                    SpeakerComponent speaker = speakerComponents[i];

                    if (speaker.State == ConnectionState.Pooled && speaker.GrainLoad < connectConfig.BusyLoadLimit)
                        continue;

                    float dist = math.distance(host.WorldPos, speaker.WorldPos);

                    if (speaker.ConnectionRadius < dist)
                        continue;

                    if (speaker.State == ConnectionState.Lingering)
                    {
                        newSpeakerIndex = speakerIndexes[i].Value;
                        break;
                    }

                    lowestActiveGrainLoad = speaker.GrainLoad;
                    newSpeakerIndex = speakerIndexes[i].Value;
                    break;
                }

                if (newSpeakerIndex == int.MaxValue)
                    return;

                host.SpeakerIndex = newSpeakerIndex;
                host.Connected = true;
                ecb.AddComponent(entityInQueryIndex, entity, new ConnectedTag());
            }
        ).WithDisposeOnCompletion(speakerIndexes).WithDisposeOnCompletion(speakerComponents)
        .ScheduleParallel(groupLoneHostsJob);
        connectToActiveSpeakerJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(connectToActiveSpeakerJob);

        hostQuery = GetEntityQuery(frameConnectedQueryDesc);
        frameComponents = hostQuery.ToComponentDataArray<HostComponent>(Allocator.TempJob);
        JobHandle moveSpeakersJob = Entities.WithName("MoveSpeakers").WithReadOnly(frameComponents).ForEach
        (
            (ref SpeakerComponent speaker, in SpeakerIndex index) =>
            {
                int attachedHosts = 0;
                float3 currentPosition = speaker.WorldPos;
                float3 targetPosition = new(0, 0, 0);
                
                for (int e = 0; e < frameComponents.Length; e++)
                {
                    if (frameComponents[e].SpeakerIndex != index.Value)
                        continue;

                    if (math.distance(currentPosition, frameComponents[e].WorldPos) > speaker.ConnectionRadius)
                    {
                        continue;
                    }

                    targetPosition += frameComponents[e].WorldPos;
                    attachedHosts++;
                }

                speaker.ConnectedHostCount = attachedHosts;

                if (attachedHosts == 0)
                {
                    speaker.InactiveDuration += connectConfig.DeltaTime;
                    if (speaker.State == ConnectionState.Lingering && speaker.InactiveDuration >= connectConfig.SpeakerLingerTime)
                    {
                        speaker.State = ConnectionState.Pooled;
                        speaker.ConnectionRadius = 0.001f;
                        speaker.WorldPos = connectConfig.DisconnectedPosition;
                    }
                    else if (speaker.State == ConnectionState.Active)
                    {
                        speaker.State = ConnectionState.Lingering;
                        speaker.InactiveDuration = 0;
                        speaker.WorldPos = currentPosition;
                        speaker.ConnectionRadius = CalculateSpeakerRadius(connectConfig.ListenerPos, currentPosition, connectConfig.ArcDegrees);
                    }
                    return;
                }

                if (speaker.State != ConnectionState.Active)
                {
                    speaker.InactiveDuration = 0;
                    speaker.State = ConnectionState.Active;
                }

                speaker.InactiveDuration -= connectConfig.DeltaTime;

                targetPosition = attachedHosts == 1 ? targetPosition : targetPosition / attachedHosts;
                float3 lerpedPosition = math.lerp(currentPosition, targetPosition, connectConfig.TranslationSmoothing);
                float lerpedRadius = CalculateSpeakerRadius(connectConfig.ListenerPos, lerpedPosition, connectConfig.ArcDegrees);

                if (lerpedRadius > math.distance(lerpedPosition, targetPosition))
                {
                    speaker.WorldPos = lerpedPosition;
                    speaker.ConnectionRadius = lerpedRadius;
                }
                else
                {
                    speaker.WorldPos = targetPosition;
                    speaker.ConnectionRadius = CalculateSpeakerRadius(connectConfig.ListenerPos, targetPosition, connectConfig.ArcDegrees);
                }
            }
        ).WithDisposeOnCompletion(frameComponents)
        .ScheduleParallel(groupLoneHostsJob);
        moveSpeakersJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(moveSpeakersJob);

        hostQuery = GetEntityQuery(frameDisconnectedQueryDesc);
        frameEntities = hostQuery.ToEntityArray(Allocator.TempJob);
        speakerEntities = speakerQuery.ToEntityArray(Allocator.TempJob);
        speakerComponents = speakerQuery.ToComponentDataArray<SpeakerComponent>(Allocator.TempJob);        
        JobHandle speakerActivationJob = Job.WithName("speakerActivation").WithoutBurst().WithCode(() =>
        {
            for (int h = 0; h < frameEntities.Length; h++)
            {
                for (int s = 0; s < speakerComponents.Length; s++)
                {
                    if (speakerComponents[s].State != ConnectionState.Pooled)
                        continue;

                    // Tick host component with speaker link
                    ecb.AddComponent(h, frameEntities, new ConnectedTag());
                    ecb.AddComponent(h, frameEntities, new LoneHostOnSpeakerTag());
                    HostComponent host = GetComponent<HostComponent>(frameEntities[h]);
                    host.SpeakerIndex = GetComponent<SpeakerIndex>(speakerEntities[s]).Value;
                    host.Connected = true;
                    SetComponent(frameEntities[h], host);

                    // Set active pooled status and update attachment radius
                    SpeakerComponent speakerComponent = GetComponent<SpeakerComponent>(speakerEntities[s]);
                    speakerComponent.ConnectionRadius = CalculateSpeakerRadius(connectConfig.ListenerPos, host.WorldPos, connectConfig.ArcDegrees);
                    speakerComponent.State = ConnectionState.Active;
                    speakerComponent.ConnectedHostCount = 1;
                    speakerComponent.InactiveDuration = 0;
                    speakerComponent.WorldPos = host.WorldPos;
                    SetComponent(speakerEntities[s], speakerComponent);
                    return;
                }
            }
        }).WithDisposeOnCompletion(speakerEntities).WithDisposeOnCompletion(speakerComponents)
        .WithDisposeOnCompletion(frameEntities)
        .Schedule(moveSpeakersJob);
        speakerActivationJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(speakerActivationJob);

        Dependency = speakerActivationJob;
    }

    public static float CalculateSpeakerRadius(float3 listenerPos, float3 speakerPos, float arcLength)
    {
        // Tick attachment radius for new position. NOTE/TODO: Radius will be incorrect for next frame, need to investigate.
        float listenerCircumference = (float)(2 * Math.PI * math.distance(listenerPos, speakerPos));
        return arcLength * MaxMath.ONE_DEGREE * listenerCircumference;
    }
}
