using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System;

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
        ConnectionConfig connectionConfig = GetSingleton<ConnectionConfig>();
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
        speakerComponents = speakerQuery.ToComponentDataArray<SpeakerComponent>(Allocator.TempJob);
        JobHandle updateHostRangeJob = Entities.WithName("UpdateHostRange").WithReadOnly(speakerComponents).ForEach
        (
            (int entityInQueryIndex, Entity entity, ref HostComponent host) =>
            {
                if (math.distance(host._WorldPos, connectionConfig._ListenerPos) > connectionConfig._ListenerRadius)
                {
                    host._Connected = false;
                    host._InListenerRadius = false;
                    ecb.RemoveComponent<ConnectedTag>(entityInQueryIndex, entity);
                    ecb.RemoveComponent<InListenerRadiusTag>(entityInQueryIndex, entity);
                    ecb.RemoveComponent<LoneHostOnSpeakerTag>(entityInQueryIndex, entity);
                    host._SpeakerIndex = int.MaxValue;
                    return;
                }

                host._InListenerRadius = true;
                ecb.AddComponent(entityInQueryIndex, entity, new InListenerRadiusTag());

                if (host._SpeakerIndex >= speakerComponents.Length || math.distance(host._WorldPos,
                    speakerComponents[host._SpeakerIndex]._WorldPos) > speakerComponents[host._SpeakerIndex]._ConnectionRadius)
                {
                    host._Connected = false;
                    host._SpeakerIndex = int.MaxValue;
                    ecb.RemoveComponent<ConnectedTag>(entityInQueryIndex, entity);
                    ecb.RemoveComponent<LoneHostOnSpeakerTag>(entityInQueryIndex, entity);
                    return;
                }

                if (speakerComponents[host._SpeakerIndex]._ConnectedHostCount == 1)
                    ecb.AddComponent(entityInQueryIndex, entity, new LoneHostOnSpeakerTag());
                ecb.AddComponent(entityInQueryIndex, entity, new ConnectedTag());
                host._Connected = true;
            }
        ).ScheduleParallel(Dependency);
        updateHostRangeJob.Complete();
        _CommandBufferSystem.AddJobHandleForProducer(updateHostRangeJob);

        speakerQuery = GetEntityQuery(speakerQueryDesc);
        speakerIndexes = speakerQuery.ToComponentDataArray<SpeakerIndex>(Allocator.TempJob);
        JobHandle groupLoneHostsJob = Entities.WithName("GroupLoneHosts")
            .WithAll<InListenerRadiusTag, ConnectedTag, LoneHostOnSpeakerTag>()
            .WithReadOnly(speakerIndexes).WithReadOnly(speakerComponents).ForEach
        (
            (int entityInQueryIndex, Entity entity, ref HostComponent host) =>
            {
                if (host._SpeakerIndex > speakerComponents.Length)
                    return;

                int otherIndex = int.MaxValue;
                float currentInactiveDuration = float.MaxValue;
                float otherSpeakerInactiveDuration = float.MaxValue;

                for (int s = 0; s < speakerComponents.Length; s++)
                {
                    if (speakerComponents[s]._State != ConnectionState.Active)
                        continue;

                    if (speakerIndexes[s].Value == host._SpeakerIndex)
                    {
                        currentInactiveDuration = speakerComponents[s]._InactiveDuration;
                        if (otherSpeakerInactiveDuration < 0)
                            break;
                    }
                    else if (speakerComponents[s]._ConnectedHostCount > 0 && 
                        math.distance(host._WorldPos, speakerComponents[s]._WorldPos) < speakerComponents[s]._ConnectionRadius)
                    {
                        otherIndex = speakerIndexes[s].Value;
                        otherSpeakerInactiveDuration = speakerComponents[s]._InactiveDuration;
                        if (currentInactiveDuration < 0)
                            break;
                    }
                }

                if (currentInactiveDuration > 0 || otherIndex > speakerComponents.Length)
                    return;

                if (otherSpeakerInactiveDuration < currentInactiveDuration)
                {
                    host._SpeakerIndex = otherIndex;
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
                int bestLingeringSpeaker = int.MaxValue;

                float lowestActiveGrainLoad = 1;
                float closestLingeringDistance = float.MaxValue;

                for (int i = 0; i < speakerComponents.Length; i++)
                {
                    SpeakerComponent speaker = speakerComponents[i];
                    if (speaker._State == ConnectionState.Pooled)
                        continue;

                    float dist = math.distance(host._WorldPos, speaker._WorldPos);
                    if (dist > speaker._ConnectionRadius)
                        continue;

                    if (speaker._State == ConnectionState.Lingering)
                    {
                        if (dist < closestLingeringDistance)
                        {
                            closestLingeringDistance = dist;
                            bestLingeringSpeaker = speakerIndexes[i].Value;
                        }
                    }
                    else if (speaker._GrainLoad < connectionConfig._BusyLoadLimit && speaker._GrainLoad < lowestActiveGrainLoad)
                    {
                        lowestActiveGrainLoad = speaker._GrainLoad;
                        newSpeakerIndex = speakerIndexes[i].Value;
                    }
                }

                if (newSpeakerIndex == int.MaxValue && bestLingeringSpeaker == int.MaxValue)
                    return;

                host._SpeakerIndex = newSpeakerIndex != int.MaxValue ? newSpeakerIndex : bestLingeringSpeaker;
                host._Connected = true;
                ecb.AddComponent(entityInQueryIndex, entity, new ConnectedTag());
            }
        ).WithDisposeOnCompletion(speakerComponents).WithDisposeOnCompletion(speakerIndexes)
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
                float3 currentPosition = speaker._WorldPos;
                float3 targetPosition = new(0, 0, 0);
                
                for (int e = 0; e < hostComponents.Length; e++)
                {
                    if (hostComponents[e]._SpeakerIndex != index.Value)
                        continue;

                    targetPosition += hostComponents[e]._WorldPos;
                    attachedHosts++;
                }

                if (attachedHosts == 0)
                {
                    speaker._InactiveDuration += connectionConfig._DeltaTime;
                    if (speaker._State == ConnectionState.Lingering && speaker._InactiveDuration >= connectionConfig._SpeakerLingerTime)
                    {
                        speaker._State = ConnectionState.Pooled;
                        speaker._ConnectionRadius = 0.001f;
                        speaker._WorldPos = connectionConfig._DisconnectedPosition;
                    }
                    else if (speaker._State == ConnectionState.Active)
                    {
                        speaker._State = ConnectionState.Lingering;
                        speaker._InactiveDuration = 0;
                        speaker._WorldPos = currentPosition;
                        speaker._ConnectionRadius = CalculateSpeakerRadius(connectionConfig._ListenerPos, currentPosition, connectionConfig._ArcDegrees);
                    }
                    speaker._ConnectedHostCount = attachedHosts;
                    return;
                }

                if (attachedHosts > 1)
                {
                    float3 averagePosition = targetPosition / attachedHosts;
                    float3 lerpedPosition = math.lerp(targetPosition, averagePosition, connectionConfig._TranslationSmoothing);
                    targetPosition = math.distance(currentPosition, lerpedPosition) > speaker._ConnectionRadius ? averagePosition : lerpedPosition;
                }

                if (speaker._State != ConnectionState.Active)
                {
                    speaker._InactiveDuration = 0;
                    speaker._State = ConnectionState.Active;
                }

                speaker._InactiveDuration -= connectionConfig._DeltaTime;
                speaker._WorldPos = targetPosition;
                speaker._ConnectedHostCount = attachedHosts;
                speaker._ConnectionRadius = CalculateSpeakerRadius(connectionConfig._ListenerPos, targetPosition, connectionConfig._ArcDegrees);
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
                    if (speakerComponents[s]._State != ConnectionState.Pooled)
                        continue;

                    // Update host component with speaker link
                    ecb.AddComponent(h, hostEntities, new ConnectedTag());
                    ecb.AddComponent(h, hostEntities, new LoneHostOnSpeakerTag());
                    HostComponent host = GetComponent<HostComponent>(hostEntities[h]);
                    host._SpeakerIndex = GetComponent<SpeakerIndex>(speakerEntities[s]).Value;
                    host._Connected = true;
                    SetComponent(hostEntities[h], host);

                    // Set active pooled status and update attachment radius
                    SpeakerComponent speakerComponent = GetComponent<SpeakerComponent>(speakerEntities[s]);
                    speakerComponent._ConnectionRadius = CalculateSpeakerRadius(connectionConfig._ListenerPos, host._WorldPos, connectionConfig._ArcDegrees);
                    speakerComponent._State = ConnectionState.Active;
                    speakerComponent._ConnectedHostCount = 1;
                    speakerComponent._InactiveDuration = 0;
                    speakerComponent._WorldPos = host._WorldPos;
                    SetComponent(speakerEntities[s], speakerComponent);
                    break;
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
        // Update attachment radius for new position. NOTE/TODO: Radius will be incorrect for next frame, need to investigate.
        float listenerCircumference = (float)(2 * Math.PI * math.distance(listenerPos, speakerPos));
        return arcLength / 360 * listenerCircumference;
    }
}
