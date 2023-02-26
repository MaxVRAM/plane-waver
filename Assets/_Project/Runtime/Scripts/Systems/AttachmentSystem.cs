using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System;

using MaxVRAM;

// https://docs.unity3d.com/Packages/com.unity.entities@0.51/api/

/// <summary>
//     Processes dynamic emitter host <-> speaker link components amd updates entity in-range statuses.
/// <summary>
[UpdateAfter(typeof(DOTS_QuadrantSystem))]
public partial class AttachmentSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _CommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        AudioTimerComponent dspTimer = GetSingleton<AudioTimerComponent>();
        ConnectionConfig attachConfig = GetSingleton<ConnectionConfig>();
        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter ecb = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();


        EntityQueryDesc hostQueryDesc = new()
        {
            All = new ComponentType[] { typeof(HostComponent)}
        };

        EntityQueryDesc hostConnectedQueryDesc = new()
        {
            All = new ComponentType[] { typeof(HostComponent), typeof(ConnectedTag) }
        };

        EntityQueryDesc hostAloneQueryDesc = new()
        {
            All = new ComponentType[] { typeof(HostComponent), typeof(ConnectedTag), typeof(LoneHostOnSpeakerTag) }
        };


        EntityQueryDesc hostNotAloneQueryDesc = new()
        {
            All = new ComponentType[] { typeof(HostComponent), typeof(ConnectedTag) },
            None = new ComponentType[] { typeof(LoneHostOnSpeakerTag) }
        };

        EntityQueryDesc hostDisconnectedQueryDesc = new()
        {
            All = new ComponentType[] { typeof(HostComponent), typeof(InListenerRadiusTag) },
            None = new ComponentType[] { typeof(ConnectedTag)}
        };
        EntityQueryDesc speakerQueryDesc = new()
        {
            All = new ComponentType[] { typeof(SpeakerIndex), typeof(SpeakerComponent) }
        };

        EntityQuery hostQuery;
        EntityQuery speakerQuery;

        NativeArray<Entity> hostEntities;
        NativeArray<Entity> speakerEntities;
        NativeArray<SpeakerIndex> speakerIndexes;
        NativeArray<HostComponent> hostComponents;
        NativeArray<SpeakerComponent> speakerComponents;

        speakerQuery = GetEntityQuery(speakerQueryDesc);
        speakerIndexes = speakerQuery.ToComponentDataArray<SpeakerIndex>(Allocator.TempJob);
        speakerComponents = speakerQuery.ToComponentDataArray<SpeakerComponent>(Allocator.TempJob);
        JobHandle updateHostRangeJob = Entities.WithName("UpdateHostRange").WithReadOnly(speakerComponents).ForEach
        (
            (int entityInQueryIndex, Entity entity, ref HostComponent host) =>
            {
                if (math.distance(host.WorldPos, attachConfig.ListenerPos) > attachConfig.ListenerRadius)
                {
                    host.Connected = false;
                    host.InListenerRadius = false;
                    ecb.RemoveComponent<ConnectedTag>(entityInQueryIndex, entity);
                    ecb.RemoveComponent<InListenerRadiusTag>(entityInQueryIndex, entity);
                    ecb.RemoveComponent<LoneHostOnSpeakerTag>(entityInQueryIndex, entity);
                    host.SpeakerIndex = int.MaxValue;
                    return;
                }

                host.InListenerRadius = true;
                ecb.AddComponent(entityInQueryIndex, entity, new InListenerRadiusTag());

                if (host.SpeakerIndex < speakerComponents.Length)
                {
                    for (int s = 0;  s < speakerComponents.Length; s++)
                    {
                        if (host.SpeakerIndex != s)
                            continue;

                        SpeakerComponent speaker = speakerComponents[s];

                        if (math.distance(host.WorldPos, speaker.WorldPos) <= speaker.ConnectionRadius)
                        {
                            if (speakerComponents[host.SpeakerIndex].ConnectedHostCount == 1)
                                ecb.AddComponent(entityInQueryIndex, entity, new LoneHostOnSpeakerTag());
                            ecb.AddComponent(entityInQueryIndex, entity, new ConnectedTag());
                            host.Connected = true;
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
        updateHostRangeJob.Complete();
        _CommandBufferSystem.AddJobHandleForProducer(updateHostRangeJob);

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
        ).ScheduleParallel(updateHostRangeJob);
        groupLoneHostsJob.Complete();
        _CommandBufferSystem.AddJobHandleForProducer(groupLoneHostsJob);

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

                    if (speaker.State == ConnectionState.Pooled && speaker.GrainLoad < attachConfig.BusyLoadLimit)
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
        _CommandBufferSystem.AddJobHandleForProducer(connectToActiveSpeakerJob);

        hostQuery = GetEntityQuery(hostConnectedQueryDesc);
        hostComponents = hostQuery.ToComponentDataArray<HostComponent>(Allocator.TempJob);
        JobHandle moveSpeakersJob = Entities.WithName("MoveSpeakers").WithReadOnly(hostComponents).ForEach
        (
            (ref SpeakerComponent speaker, in SpeakerIndex index) =>
            {
                int attachedHosts = 0;
                float3 currentPosition = speaker.WorldPos;
                float3 targetPosition = new(0, 0, 0);
                
                for (int e = 0; e < hostComponents.Length; e++)
                {
                    if (hostComponents[e].SpeakerIndex != index.Value)
                        continue;

                    if (math.distance(currentPosition, hostComponents[e].WorldPos) > speaker.ConnectionRadius)
                    {
                        continue;
                    }

                    targetPosition += hostComponents[e].WorldPos;
                    attachedHosts++;
                }

                speaker.ConnectedHostCount = attachedHosts;

                if (attachedHosts == 0)
                {
                    speaker.InactiveDuration += attachConfig.DeltaTime;
                    if (speaker.State == ConnectionState.Lingering && speaker.InactiveDuration >= attachConfig.SpeakerLingerTime)
                    {
                        speaker.State = ConnectionState.Pooled;
                        speaker.ConnectionRadius = 0.001f;
                        speaker.WorldPos = attachConfig.DisconnectedPosition;
                    }
                    else if (speaker.State == ConnectionState.Active)
                    {
                        speaker.State = ConnectionState.Lingering;
                        speaker.InactiveDuration = 0;
                        speaker.WorldPos = currentPosition;
                        speaker.ConnectionRadius = CalculateSpeakerRadius(attachConfig.ListenerPos, currentPosition, attachConfig.ArcDegrees);
                    }
                    return;
                }

                if (speaker.State != ConnectionState.Active)
                {
                    speaker.InactiveDuration = 0;
                    speaker.State = ConnectionState.Active;
                }

                speaker.InactiveDuration -= attachConfig.DeltaTime;

                targetPosition = attachedHosts == 1 ? targetPosition : targetPosition / attachedHosts;
                float3 lerpedPosition = math.lerp(currentPosition, targetPosition, attachConfig.TranslationSmoothing);
                float lerpedRadius = CalculateSpeakerRadius(attachConfig.ListenerPos, lerpedPosition, attachConfig.ArcDegrees);

                if (lerpedRadius > math.distance(lerpedPosition, targetPosition))
                {
                    speaker.WorldPos = lerpedPosition;
                    speaker.ConnectionRadius = lerpedRadius;
                }
                else
                {
                    speaker.WorldPos = targetPosition;
                    speaker.ConnectionRadius = CalculateSpeakerRadius(attachConfig.ListenerPos, targetPosition, attachConfig.ArcDegrees);
                }
            }
        ).WithDisposeOnCompletion(hostComponents)
        .ScheduleParallel(groupLoneHostsJob);
        moveSpeakersJob.Complete();
        _CommandBufferSystem.AddJobHandleForProducer(moveSpeakersJob);

        hostQuery = GetEntityQuery(hostDisconnectedQueryDesc);
        hostEntities = hostQuery.ToEntityArray(Allocator.TempJob);
        speakerEntities = speakerQuery.ToEntityArray(Allocator.TempJob);
        speakerComponents = speakerQuery.ToComponentDataArray<SpeakerComponent>(Allocator.TempJob);        
        JobHandle speakerActivationJob = Job.WithName("speakerActivation").WithoutBurst().WithCode(() =>
        {
            for (int h = 0; h < hostEntities.Length; h++)
            {
                for (int s = 0; s < speakerComponents.Length; s++)
                {
                    if (speakerComponents[s].State != ConnectionState.Pooled)
                        continue;

                    // Tick host component with speaker link
                    ecb.AddComponent(h, hostEntities, new ConnectedTag());
                    ecb.AddComponent(h, hostEntities, new LoneHostOnSpeakerTag());
                    HostComponent host = GetComponent<HostComponent>(hostEntities[h]);
                    host.SpeakerIndex = GetComponent<SpeakerIndex>(speakerEntities[s]).Value;
                    host.Connected = true;
                    SetComponent(hostEntities[h], host);

                    // Set active pooled status and update attachment radius
                    SpeakerComponent speakerComponent = GetComponent<SpeakerComponent>(speakerEntities[s]);
                    speakerComponent.ConnectionRadius = CalculateSpeakerRadius(attachConfig.ListenerPos, host.WorldPos, attachConfig.ArcDegrees);
                    speakerComponent.State = ConnectionState.Active;
                    speakerComponent.ConnectedHostCount = 1;
                    speakerComponent.InactiveDuration = 0;
                    speakerComponent.WorldPos = host.WorldPos;
                    SetComponent(speakerEntities[s], speakerComponent);
                    return;
                }
            }
        }).WithDisposeOnCompletion(speakerEntities).WithDisposeOnCompletion(speakerComponents)
        .WithDisposeOnCompletion(hostEntities)
        .Schedule(moveSpeakersJob);
        speakerActivationJob.Complete();
        _CommandBufferSystem.AddJobHandleForProducer(speakerActivationJob);

        Dependency = speakerActivationJob;
    }

    public static float CalculateSpeakerRadius(float3 listenerPos, float3 speakerPos, float arcLength)
    {
        // Tick attachment radius for new position. NOTE/TODO: Radius will be incorrect for next frame, need to investigate.
        float listenerCircumference = (float)(2 * Math.PI * math.distance(listenerPos, speakerPos));
        return arcLength * MaxMath.ONE_DEGREE * listenerCircumference;
    }
}
