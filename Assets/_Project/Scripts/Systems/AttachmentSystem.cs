using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter ecb = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        EntityQueryDesc speakerQueryDesc = new()
        {
            All = new ComponentType[] { typeof(SpeakerIndex), typeof(SpeakerComponent), typeof(Translation) }
        };
        EntityQuery speakersQuery = GetEntityQuery(speakerQueryDesc);



        EntityQueryDesc hostQueryDesc = new()
        {
            All = new ComponentType[] { typeof(HostComponent), typeof(Translation) }
        };

        EntityQueryDesc hostConnectedQueryDesc = new()
        {
            All = new ComponentType[] { typeof(HostComponent), typeof(Translation), typeof(ConnectedTag) }
        };

        EntityQueryDesc hostDisconnectedQueryDesc = new()
        {
            All = new ComponentType[] { typeof(HostComponent), typeof(Translation), typeof(InListenerRadiusTag) },
            None = new ComponentType[] { typeof(ConnectedTag)}
        };

        EntityQuery hostsQuery;

        AudioTimerComponent dspTimer = GetSingleton<AudioTimerComponent>();       
        ConnectionConfig connectionConfig = GetSingleton<ConnectionConfig>();
                



        //----    UPDATE HOST IN-RANGE STATUSES
        NativeArray<SpeakerComponent> speakerPool = speakersQuery.ToComponentDataArray<SpeakerComponent>(Allocator.TempJob);
        NativeArray<Translation> speakerTranslations = speakersQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        JobHandle updateHostRangeJob = Entities.WithName("UpdateHostRange").WithReadOnly(speakerPool).WithReadOnly(speakerTranslations).ForEach
        (
            (int entityInQueryIndex, Entity entity, ref HostComponent host, in Translation translation) =>
            {
                if (math.distance(translation.Value, connectionConfig._ListenerPos) > connectionConfig._ListenerRadius)
                {
                    host._Connected = false;
                    host._SpeakerIndex = int.MaxValue;
                    host._InListenerRadius = false;
                    ecb.RemoveComponent<InListenerRadiusTag>(entityInQueryIndex, entity);
                }
                else
                {
                    host._InListenerRadius = true;
                    ecb.AddComponent(entityInQueryIndex, entity, new InListenerRadiusTag());
                }

                if (host._SpeakerIndex < speakerPool.Length && speakerPool[host._SpeakerIndex]._State != ConnectionState.Pooled)
                {
                    if (math.distance(translation.Value, speakerTranslations[host._SpeakerIndex].Value) <
                        speakerPool[host._SpeakerIndex]._ConnectionRadius)
                    {
                        host._Connected = true;
                        ecb.AddComponent(entityInQueryIndex, entity, new ConnectedTag());
                    }
                }
                else
                {
                    host._Connected = false;
                    host._SpeakerIndex = int.MaxValue;
                    ecb.RemoveComponent<ConnectedTag>(entityInQueryIndex, entity);
                }
            }
        ).WithDisposeOnCompletion(speakerPool)
        .WithDisposeOnCompletion(speakerTranslations)
        .ScheduleParallel(Dependency);
        updateHostRangeJob.Complete();

        _CommandBufferSystem.AddJobHandleForProducer(updateHostRangeJob);




        ////----    CONNECT DISCONNECTED HOST TO IN-RANGE SPEAKER
        //// TODO: test if better to remove speaker connection state check - potentially reposition newly pooled speakers on the main thread
        //NativeArray<Entity> inRangeSpeaker = speakersQuery.ToEntityArray(Allocator.TempJob);
        //JobHandle connectToActiveSpeakerJob = Entities.WithName("LinkToActiveSpeaker").
        //    WithNone<ConnectedTag>().WithAll<InListenerRadiusTag>().WithReadOnly(inRangeSpeaker).ForEach
        //(
        //    (int entityInQueryIndex, Entity entity, ref HostComponent host, in Translation translation) =>
        //    {
        //        int closestSpeakerIndex = int.Max;
        //        float closestDist = float.Max;

        //        for (int i = 0; i < inRangeSpeaker.Length; i++)
        //        {
        //            SpeakerComponent speaker = GetComponent<SpeakerComponent>(inRangeSpeaker[i]);
        //            if (speaker._State != ConnectionState.Pooled)
        //            {
        //                float dist = math.distance(translation.Value, GetComponent<Translation>(inRangeSpeaker[i]).Value);
        //                if (dist < speaker._ConnectionRadius)
        //                {
        //                    closestDist = dist;
        //                    closestSpeakerIndex = GetComponent<SpeakerIndex>(inRangeSpeaker[i]).Value;
        //                }
        //            }
        //        }

        //        if (closestSpeakerIndex != int.Max)
        //        {
        //            host._Connected = true;
        //            host._SpeakerIndex = closestSpeakerIndex;
        //            ecb.AddComponent(entityInQueryIndex, entity, new ConnectedTag());
        //        }
        //    }
        //).WithDisposeOnCompletion(inRangeSpeaker)
        //.ScheduleParallel(updateHostRangeJob);
        //connectToActiveSpeakerJob.Complete();

        //_CommandBufferSystem.AddJobHandleForProducer(connectToActiveSpeakerJob);



        //----    CONNECT DISCONNECTED HOST TO BEST IN-RANGE ACTIVE SPEAKER
        NativeArray<Entity> inRangeSpeaker = speakersQuery.ToEntityArray(Allocator.TempJob);
        JobHandle connectToActiveSpeakerJob = Entities.WithName("LinkToActiveSpeaker").
            WithNone<ConnectedTag>().WithAll<InListenerRadiusTag>().WithReadOnly(inRangeSpeaker).ForEach
        (
            (int entityInQueryIndex, Entity entity, ref HostComponent host, in Translation translation) =>
            {
                int bestSpeakerIndex = int.MaxValue;
                int bestSpeakerHosts = -1;
                float bestSpeakerGrainLoad = 1;

                for (int i = 0; i < inRangeSpeaker.Length; i++)
                {
                    SpeakerComponent speaker = GetComponent<SpeakerComponent>(inRangeSpeaker[i]);
                    if (speaker._State == ConnectionState.Active && speaker._GrainLoad < connectionConfig._BusyLoadLimit && speaker._ConnectedHostCount > bestSpeakerHosts)
                    {
                        float dist = math.distance(translation.Value, GetComponent<Translation>(inRangeSpeaker[i]).Value);
                        if (dist < speaker._ConnectionRadius)
                        {
                            bestSpeakerGrainLoad = speaker._GrainLoad;
                            bestSpeakerHosts = speaker._ConnectedHostCount;
                            bestSpeakerIndex = GetComponent<SpeakerIndex>(inRangeSpeaker[i]).Value;
                        }
                    }
                }
                if (bestSpeakerIndex != int.MaxValue)
                {
                    host._Connected = true;
                    host._SpeakerIndex = bestSpeakerIndex;
                    ecb.AddComponent(entityInQueryIndex, entity, new ConnectedTag());
                }
            }
        ).WithDisposeOnCompletion(inRangeSpeaker)
        .ScheduleParallel(updateHostRangeJob);
        connectToActiveSpeakerJob.Complete();

        _CommandBufferSystem.AddJobHandleForProducer(connectToActiveSpeakerJob);



        //----     CALCULATE SPEAKER ATTACHMENT RADIUS, SET MOVE TO AVERAGE POSITION OF ATTACHED HOST, OR POOL IF NO LONGER ATTACHED
        hostsQuery = GetEntityQuery(hostConnectedQueryDesc);
        NativeArray<HostComponent> hostsToSitSpeakers = hostsQuery.ToComponentDataArray<HostComponent>(Allocator.TempJob);
        NativeArray<Translation> hostTranslations = hostsQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        JobHandle updateSpeakerPoolJob = Entities.WithName("MoveSpeakers").WithReadOnly(hostsToSitSpeakers).WithReadOnly(hostTranslations).ForEach
        (
            (ref Translation translation, ref SpeakerComponent speaker, in SpeakerIndex index) =>
            { 
                int attachedHosts = 0;
                float3 currentPos = translation.Value;
                float3 hostPosSum = new(0, 0, 0);
                
                for (int e = 0; e < hostsToSitSpeakers.Length; e++)
                {
                    if (hostsToSitSpeakers[e]._SpeakerIndex == index.Value)
                    {
                        hostPosSum += hostTranslations[e].Value;
                        attachedHosts++;
                    }
                }

                if (attachedHosts == 0)
                {
                    speaker._InactiveDuration += connectionConfig._DeltaTime;
                    if (speaker._State == ConnectionState.Lingering && speaker._InactiveDuration >= connectionConfig._SpeakerLingerTime)
                    {
                        speaker._State = ConnectionState.Pooled;
                        speaker._ConnectionRadius = 0.001f;
                        currentPos = connectionConfig._DisconnectedPosition;
                    }
                    else if (speaker._State == ConnectionState.Active)
                    {
                        speaker._State = ConnectionState.Lingering;
                        speaker._InactiveDuration = 0;
                        speaker._ConnectionRadius = CalculateSpeakerRadius(connectionConfig._ListenerPos, currentPos, connectionConfig._ArcDegrees);
                    }
                }
                else
                {
                    speaker._State = ConnectionState.Active;
                    speaker._InactiveDuration = 0;
                    if (speaker._ConnectedHostCount == 1)
                    {
                        currentPos = hostPosSum;
                    }
                    else
                    {
                        float3 targetPos = hostPosSum / attachedHosts;
                        float3 lerpPos = math.lerp(currentPos, targetPos, connectionConfig._TranslationSmoothing);
                        if (math.distance(currentPos, lerpPos) > speaker._ConnectionRadius)
                            currentPos = targetPos;
                        else
                            currentPos = lerpPos;
                    }

                    speaker._ConnectionRadius = CalculateSpeakerRadius(connectionConfig._ListenerPos, currentPos, connectionConfig._ArcDegrees);
                }
                speaker._ConnectedHostCount = attachedHosts;
                translation.Value = currentPos;
            }
        ).WithDisposeOnCompletion(hostsToSitSpeakers)
        .WithDisposeOnCompletion(hostTranslations)
        .ScheduleParallel(connectToActiveSpeakerJob);
        updateSpeakerPoolJob.Complete();

        _CommandBufferSystem.AddJobHandleForProducer(updateSpeakerPoolJob);



        //----     SPAWN A POOLED SPEAKER ON A HOST IF NO NEARBY SPEAKERS WERE FOUND
        // TODO: Use speaker "WithNone<ActiveTag>" to optimise search
        hostsQuery = GetEntityQuery(hostDisconnectedQueryDesc);
        NativeArray<Entity> hostEntities = hostsQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<Translation> hostTrans = hostsQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<HostComponent> hosts = hostsQuery.ToComponentDataArray<HostComponent>(Allocator.TempJob);
        NativeArray<Entity> speakerEntities = speakersQuery.ToEntityArray(Allocator.TempJob);
        NativeArray<SpeakerComponent> speakers = speakersQuery.ToComponentDataArray<SpeakerComponent>(Allocator.TempJob);        

        JobHandle speakerActivation = Job.WithName("speakerActivation").WithReadOnly(hostTrans).WithoutBurst().WithCode(() =>
        {
            for (int h = 0; h < hosts.Length; h++)
            {
                for (int s = 0; s < speakers.Length; s++)
                {
                    if (speakers[s]._State != ConnectionState.Active)
                    {
                        // Update host component with speaker link
                        ecb.AddComponent(h, hostEntities, new ConnectedTag());
                        HostComponent host = GetComponent<HostComponent>(hostEntities[h]);
                        host._SpeakerIndex = GetComponent<SpeakerIndex>(speakerEntities[s]).Value;
                        host._Connected = true;
                        SetComponent(hostEntities[h], host);

                        // Update speaker position
                        Translation speakerTranslation = GetComponent<Translation>(speakerEntities[s]);
                        speakerTranslation.Value = hostTrans[h].Value;
                        SetComponent(speakerEntities[s], speakerTranslation);

                        // Set active pooled status and update attachment radius
                        SpeakerComponent pooledObj = GetComponent<SpeakerComponent>(speakerEntities[s]);
                        pooledObj._ConnectionRadius = CalculateSpeakerRadius(connectionConfig._ListenerPos, speakerTranslation.Value, connectionConfig._ArcDegrees);
                        pooledObj._State = ConnectionState.Active;
                        pooledObj._ConnectedHostCount = 1;
                        pooledObj._InactiveDuration = 0;
                        SetComponent(speakerEntities[s], pooledObj);
                        break;
                    }
                }

            }
        }).WithDisposeOnCompletion(hostEntities).WithDisposeOnCompletion(hosts).WithDisposeOnCompletion(hostTrans)
        .WithDisposeOnCompletion(speakerEntities).WithDisposeOnCompletion(speakers)
        .Schedule(updateSpeakerPoolJob);
        speakerActivation.Complete();

        _CommandBufferSystem.AddJobHandleForProducer(speakerActivation);

        Dependency = speakerActivation;
    }

    public static float CalculateSpeakerRadius(float3 listenerPos, float3 speakerPos, float arcLength)
    {
        // Update attachment radius for new position. NOTE/TODO: Radius will be incorrect for next frame, need to investigate.
        float listenerCircumference = (float)(2 * Math.PI * math.distance(listenerPos, speakerPos));
        return arcLength / 360 * listenerCircumference;
    }
}


