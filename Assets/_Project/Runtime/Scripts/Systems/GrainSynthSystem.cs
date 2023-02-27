using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;

using MaxVRAM;

[UpdateInGroup(typeof(InitializationSystemGroup))]
class RandomSystem : ComponentSystem
{
    public NativeArray<Unity.Mathematics.Random> RandomArray { get; private set; }
    protected override void OnCreate()
    {
        var randomArray = new Unity.Mathematics.Random[JobsUtility.MaxJobThreadCount];
        var seed = new System.Random();

        for (int i = 0; i < JobsUtility.MaxJobThreadCount; ++i)
            randomArray[i] = new Unity.Mathematics.Random((uint)seed.Next());

        RandomArray = new NativeArray<Unity.Mathematics.Random>(randomArray, Allocator.Persistent);
    }
    protected override void OnDestroy()
        => RandomArray.Dispose();

    protected override void OnUpdate() { }
}

[UpdateAfter(typeof(AttachmentSystem))]
public partial class GrainSynthSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _CommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int samplesPerMS = (int)(sampleRate * .001f);

        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter ecb = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // ----------------------------------- EMITTER UPDATE
        // Get all audio clip data components
        NativeArray<AudioClipDataComponent> audioClipData = GetEntityQuery(typeof(AudioClipDataComponent)).
            ToComponentDataArray<AudioClipDataComponent>(Allocator.TempJob);

        WindowingDataComponent windowingData = GetSingleton<WindowingDataComponent>();
        AudioTimerComponent dspTimer = GetSingleton<AudioTimerComponent>();
        var randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;

        #region EMIT STABLE GRAINS
        //---   CREATES ENTITIES W/ GRAIN PROCESSOR + GRAIN SAMPLE BUFFER + DSP SAMPLE BUFFER + DSP PARAMS BUFFER
        JobHandle emitStableGrainsJob = Entities.WithName("EmitStableGrains")
            .WithNativeDisableParallelForRestriction(randomArray).WithReadOnly(audioClipData)
            .WithAll<PlayingTag, ConnectedTag, InListenerRadiusTag>().ForEach
        (
            (int nativeThreadIndex, int entityInQueryIndex, ref DynamicBuffer<DSPParametersElement> dspChain, ref ContinuousComponent emitter) => 
            {
                // Max grains to stop it getting stuck in a while loop
                int maxGrains = 50;
                int grainCount = 0;
                int dspTailLength = 0;

                // Get new random values
                var randomGen = randomArray[nativeThreadIndex];
                float randomDensity = randomGen.NextFloat(-1, 1);
                float randomDuration = randomGen.NextFloat(-1, 1);
                float randomPlayhead = randomGen.NextFloat(-1, 1);
                float randomTranspose = randomGen.NextFloat(-1, 1);
                float randomVolume = randomGen.NextFloat(-1, 1);

                // Compute first grain value
                float density = ComputeEmitterParameter(emitter.Density, randomDensity);
                int duration = (int)ComputeEmitterParameter(emitter.Duration, randomDuration) * samplesPerMS;
                int offset = 0;
                int sampleIndexNextGrainStart = dspTimer.NextFrameIndexEstimate;
                if (emitter.LastSampleIndex > 0 && emitter.PreviousGrainDuration > 0)
                {
                    offset = (int)(emitter.PreviousGrainDuration / density);
                    sampleIndexNextGrainStart = emitter.LastSampleIndex + offset;
                }
                float playhead = ComputeEmitterParameter(emitter.Playhead, randomPlayhead);
                float transpose = ComputeEmitterParameter(emitter.Transpose, randomTranspose);
                float pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));

                float fadeFactor = FadeFactor(sampleIndexNextGrainStart - dspTimer.NextFrameIndexEstimate, emitter.SamplesUntilFade, emitter.SamplesUntilDeath);
                float volume = ComputeEmitterParameter(emitter.Volume, randomVolume) * emitter.DistanceAmplitude * emitter.VolumeAdjust * fadeFactor;

                // Create new grain
                while (sampleIndexNextGrainStart <= dspTimer.NextFrameIndexEstimate + dspTimer.GrainQueueSampleDuration && grainCount < maxGrains)
                {
                    if (volume > 0.005f)
                    {
                        // Prevent infinite loop if there's too many grains for some reason
                        grainCount++;
                        // Find the largest delay DSP effect tail in the chain so that the tail can be added to the sample and DSP buffers
                        for (int j = 0; j < dspChain.Length; j++)
                            if (dspChain[j].DelayBasedEffect)
                                if (dspChain[j].SampleTail > dspTailLength)
                                    dspTailLength = dspChain[j].SampleTail;

                        dspTailLength = Mathf.Clamp(dspTailLength, 0, sampleRate - duration);
                        Entity grainProcessorEntity = ecb.CreateEntity(entityInQueryIndex);
                        // Add ping-pong tag if needed
                        int clipLength = audioClipData[emitter.AudioClipIndex].ClipDataBlobAsset.Value.Array.Length;
                        if (emitter.PingPong && playhead * clipLength + duration * pitch >= clipLength)
                            ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new PingPongTag());
                        // Build grain processor entity
                        ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new GrainComponent
                        {
                            AudioClipDataComponent = audioClipData[emitter.AudioClipIndex],
                            PlayheadNorm = playhead,
                            SampleCount = duration,
                            Pitch = pitch,
                            Volume = volume,
                            SpeakerIndex = emitter.SpeakerIndex,
                            StartSampleIndex = sampleIndexNextGrainStart,
                            EffectTailSampleLength = dspTailLength
                        });
                        // Attach sample and DSP buffers to grain processor
                        ecb.AddBuffer<GrainSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        ecb.AddBuffer<DSPSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        // Add DSP parameters to grain processor
                        DynamicBuffer<DSPParametersElement> dspParameters = ecb.AddBuffer<DSPParametersElement>(entityInQueryIndex, grainProcessorEntity);
                        for (int i = 0; i < dspChain.Length; i++)
                        {
                            DSPParametersElement tempParams = dspChain[i];
                            tempParams.SampleStartTime = sampleIndexNextGrainStart;
                            dspParameters.Add(tempParams);
                        }
                    }
                    // Remember this grain's timing values for next frame
                    emitter.LastSampleIndex = sampleIndexNextGrainStart;
                    emitter.PreviousGrainDuration = duration;
                    // Get random values for next iteration and update random array to avoid repeating values
                    randomPlayhead = randomGen.NextFloat(-1, 1);
                    randomVolume = randomGen.NextFloat(-1, 1);
                    randomTranspose = randomGen.NextFloat(-1, 1);
                    randomDuration = randomGen.NextFloat(-1, 1);
                    randomDensity = randomGen.NextFloat(-1, 1);
                    randomArray[nativeThreadIndex] = randomGen;
                    // Compute grain values for next iteration
                    duration = (int)ComputeEmitterParameter(emitter.Duration, randomDuration) * samplesPerMS;
                    density = ComputeEmitterParameter(emitter.Density, randomDensity);
                    offset = (int)(duration / density);
                    sampleIndexNextGrainStart += offset;
                    playhead = ComputeEmitterParameter(emitter.Playhead, randomPlayhead);
                    transpose = ComputeEmitterParameter(emitter.Transpose, randomTranspose);
                    pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));

                    fadeFactor = FadeFactor(sampleIndexNextGrainStart - dspTimer.NextFrameIndexEstimate, emitter.SamplesUntilFade, emitter.SamplesUntilDeath);
                    volume = ComputeEmitterParameter(emitter.Volume, randomVolume) * emitter.DistanceAmplitude * emitter.VolumeAdjust * fadeFactor;
                }
            }
        ).ScheduleParallel(Dependency);
        emitStableGrainsJob.Complete();
        _CommandBufferSystem.AddJobHandleForProducer(emitStableGrainsJob);

        #endregion

        #region EMIT VOLATILE GRAINS

        JobHandle emitVolatileGrainsJob = Entities.WithName("EmitVolatileGrains")
            .WithNativeDisableParallelForRestriction(randomArray).WithReadOnly(audioClipData)
            .WithAll<PlayingTag, ConnectedTag, InListenerRadiusTag>().ForEach
        (
            (int nativeThreadIndex, int entityInQueryIndex, Entity entity, ref DynamicBuffer<DSPParametersElement> dspChain, ref BurstComponent burst) => 
            {
                int grainsCreated = 0;
                int dspTailLength = 0;
                var randomGen = randomArray[nativeThreadIndex];
                int currentDSPTime = dspTimer.NextFrameIndexEstimate + (int)(randomGen.NextFloat(0, 1) * dspTimer.RandomiseBurstStartIndex);

                int lengthRange = (int)(burst.Length.Max - burst.Length.Min);

                int lengthModulation = (int)(MaxMath.Map(burst.Length.Input, 0, 1, 0, 1,
                    burst.Length.Exponent) * burst.Length.Modulation * lengthRange);

                int lengthRandom = (int)(randomGen.NextFloat(-1, 1) * burst.Length.Noise * lengthRange);

                int totalBurstLength = (int)Mathf.Clamp(burst.Length.StartValue + lengthModulation + lengthRandom,
                    burst.Length.Min, burst.Length.Max) * samplesPerMS;

                float randomDensity = randomGen.NextFloat(-1, 1);
                float randomDuration = randomGen.NextFloat(-1, 1);
                float randomPlayhead = randomGen.NextFloat(-1, 1);
                float randomTranspose = randomGen.NextFloat(-1, 1);
                float randomVolume = randomGen.NextFloat(-1, 1);

                // Compute first grain value
                int offset = 0;
                float density = ComputeBurstParameter(burst.Density, offset, totalBurstLength, randomDensity);
                int duration = (int)ComputeBurstParameter(burst.Duration, offset, totalBurstLength, randomDuration) * samplesPerMS;

                float playhead = ComputeBurstParameter(burst.Playhead, offset, totalBurstLength, randomPlayhead);
                float transpose = ComputeBurstParameter(burst.Transpose, offset, totalBurstLength, randomTranspose);
                float pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));

                float volume = ComputeBurstParameter(burst.Volume, offset, totalBurstLength, randomVolume) * burst.VolumeAdjust * burst.DistanceAmplitude;

                while (offset < totalBurstLength)
                {
                    if (volume > 0.005f)
                    {
                        grainsCreated++;
                        // Find the largest delay DSP effect tail in the chain so that the tail can be added to the sample and DSP buffers
                        for (int j = 0; j < dspChain.Length; j++)
                            if (dspChain[j].DelayBasedEffect)
                                if (dspChain[j].SampleTail > dspTailLength)
                                    dspTailLength = dspChain[j].SampleTail;

                        dspTailLength = Mathf.Clamp(dspTailLength, 0, sampleRate - duration);
                        Entity grainProcessorEntity = ecb.CreateEntity(entityInQueryIndex);

                        //Add ping-pong tag if needed
                        int clipLength = audioClipData[burst.AudioClipIndex].ClipDataBlobAsset.Value.Array.Length;
                        if (burst.PingPong && playhead * clipLength + duration * pitch >= clipLength)
                            ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new PingPongTag());

                        ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new GrainComponent
                        {
                            AudioClipDataComponent = audioClipData[burst.AudioClipIndex],
                            PlayheadNorm = playhead,
                            SampleCount = duration,
                            Pitch = pitch,
                            Volume = volume,
                            SpeakerIndex = burst.SpeakerIndex,
                            StartSampleIndex = currentDSPTime + offset,
                            EffectTailSampleLength = dspTailLength
                        });
                        // Attach sample and DSP buffers to grain processor
                        ecb.AddBuffer<GrainSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        ecb.AddBuffer<DSPSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        // Add DSP parameters to grain processor
                        DynamicBuffer<DSPParametersElement> dspParameters = ecb.AddBuffer<DSPParametersElement>(entityInQueryIndex, grainProcessorEntity);

                        for (int i = 0; i < dspChain.Length; i++)
                        {
                            DSPParametersElement tempParams = dspChain[i];
                            tempParams.SampleStartTime = currentDSPTime + offset;
                            dspParameters.Add(tempParams);
                        }
                    }
                    // Get random values for next iteration and update random array to avoid repeating values
                    if (!burst.Density.LockNoise)
                        randomDensity = randomGen.NextFloat(-1, 1);
                    if (!burst.Playhead.LockNoise)
                        randomPlayhead = randomGen.NextFloat(-1, 1);
                    if (!burst.Duration.LockNoise)
                        randomDuration = randomGen.NextFloat(-1, 1);
                    randomVolume = randomGen.NextFloat(-1, 1);
                    if (!burst.Transpose.LockNoise)
                        randomTranspose = randomGen.NextFloat(-1, 1);
                    randomArray[nativeThreadIndex] = randomGen;
                    // Compute grain values for next iteration
                    offset += (int)(duration / density);
                    density = ComputeBurstParameter(burst.Density, offset, totalBurstLength, randomDensity);
                    duration = (int)ComputeBurstParameter(burst.Duration, offset, totalBurstLength, randomDuration) * samplesPerMS;
                    playhead = ComputeBurstParameter(burst.Playhead, offset, totalBurstLength, randomPlayhead);
                    transpose = ComputeBurstParameter(burst.Transpose, offset, totalBurstLength, randomTranspose);
                    pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));
                    volume = ComputeBurstParameter(burst.Volume, offset, totalBurstLength, randomVolume) * burst.VolumeAdjust * burst.DistanceAmplitude;
                }
                ecb.RemoveComponent<PlayingTag>(entityInQueryIndex, entity);
                burst.IsPlaying = false;
            }
        ).WithDisposeOnCompletion(audioClipData)
        .ScheduleParallel(emitStableGrainsJob);
        emitVolatileGrainsJob.Complete();
        _CommandBufferSystem.AddJobHandleForProducer(emitVolatileGrainsJob);

        #endregion

        #region POPULATE GRAINS
        // ----------------------------------- GRAIN PROCESSOR UPDATE
        //---   TAKES GRAIN PROCESSOR INFORMATION AND FILLS THE SAMPLE BUFFER + DSP BUFFER (W/ 0s TO THE SAME LENGTH AS SAMPLE BUFFER)
        //NativeArray<Entity> grainProcessorEntities = GetEntityQuery(typeof(GrainComponent)).ToEntityArray(Allocator.TempJob);
        JobHandle processGrainSamplesJob = Entities.WithName("ProcessGrainSamples")
            .WithNone<PingPongTag, SamplesProcessedTag>().ForEach
        (
            (int entityInQueryIndex, Entity entity, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer,
            DynamicBuffer<DSPSampleBufferElement> dspBuffer, in GrainComponent grain) =>
            {
                ref BlobArray<float> clipArray = ref grain.AudioClipDataComponent.ClipDataBlobAsset.Value.Array;
                float sourceIndex = grain.PlayheadNorm * clipArray.Length;
                float increment = grain.Pitch;

                for (int i = 0; i < grain.SampleCount - 1; i++)
                {
                    // Set rate of sample read to alter pitch - interpolate sample if not integer to create 
                    sourceIndex += increment;

                    if (sourceIndex + 1 >= clipArray.Length)
                    {
                        for (int j = i; j < grain.SampleCount - 1; j++)
                            sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = 0 });
                        break;
                    }

                    float sourceIndexRemainder = sourceIndex % 1;
                    float sourceValue;

                    if (sourceIndexRemainder != 0)
                        sourceValue = math.lerp(clipArray[(int)sourceIndex], clipArray[(int)sourceIndex + 1], sourceIndexRemainder);
                    else
                        sourceValue = clipArray[(int)sourceIndex];
                    // Adjusted for volume and windowing
                    sourceValue *= grain.Volume;
                    sourceValue *= windowingData.WindowingArray.Value.Array[
                        (int)MaxMath.Map(i, 0, grain.SampleCount, 0, windowingData.WindowingArray.Value.Array.Length)];
                    sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = sourceValue });
                    dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                }
                // --Add additional samples to increase grain playback size based on DSP effect tail length
                for (int i = 0; i < grain.EffectTailSampleLength; i++)
                {
                    sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = 0 });
                    dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                }
                ecb.AddComponent(entityInQueryIndex, entity, new SamplesProcessedTag());
            }
        ).ScheduleParallel(emitVolatileGrainsJob);
        processGrainSamplesJob.Complete();
        _CommandBufferSystem.AddJobHandleForProducer(processGrainSamplesJob);

        #endregion

        #region POPULATE PiNG PONG GRAINS
        //-----------------------------------GRAIN PROCESSOR UPDATE
        //---TAKES GRAIN PROCESSOR INFORMATION AND FILLS THE SAMPLE BUFFER +DSP BUFFER(W / 0s TO THE SAME LENGTH AS SAMPLE BUFFER)
        JobHandle processPingPongSamplesJob = Entities.WithName("ProcessPingPongSamples")
            .WithAll<PingPongTag>().WithNone<SamplesProcessedTag>().ForEach
        (
            (int entityInQueryIndex, Entity entity, DynamicBuffer <GrainSampleBufferElement> sampleOutputBuffer,
                DynamicBuffer<DSPSampleBufferElement> dspBuffer, in GrainComponent grain) =>
            {
                ref BlobArray<float> clipArray = ref grain.AudioClipDataComponent.ClipDataBlobAsset.Value.Array;
                float sourceIndex = grain.PlayheadNorm * clipArray.Length;
                float increment = grain.Pitch;

                for (int i = 0; i < grain.SampleCount; i++)
                {
                    // Ping pong samples at the limit of the sample
                    if (sourceIndex + increment < 0 || sourceIndex + increment > clipArray.Length - 1)
                        increment = -increment;
                    // Set rate of sample read to alter pitch - interpolate sample if not integer to create 
                    sourceIndex += increment;

                    float sourceIndexRemainder = sourceIndex % 1;
                    float sourceValue;

                    if (sourceIndexRemainder != 0)
                        sourceValue = math.lerp(clipArray[(int)sourceIndex], clipArray[(int)sourceIndex + 1], sourceIndexRemainder);
                    else
                        sourceValue = clipArray[(int)sourceIndex];
                    // Adjusted for volume and windowing
                    sourceValue *= grain.Volume;
                    sourceValue *= windowingData.WindowingArray.Value.Array[
                        (int)MaxMath.Map(i, 0, grain.SampleCount, 0, windowingData.WindowingArray.Value.Array.Length)];
                    sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = sourceValue });
                    dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                }

                // --Add additional samples to increase grain playback size based on DSP effect tail length
                for (int i = 0; i < grain.EffectTailSampleLength; i++)
                {
                    sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = 0 });
                    dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                }
                ecb.AddComponent(entityInQueryIndex, entity, new SamplesProcessedTag());
            }
        ).ScheduleParallel(emitVolatileGrainsJob);
        processPingPongSamplesJob.Complete();
        _CommandBufferSystem.AddJobHandleForProducer(processPingPongSamplesJob);

        #endregion

        #region DSP CHAIN        

        JobHandle dspEffectSampleJob = Entities.WithName("DSPEffectSampleJob").WithAll<SamplesProcessedTag>().ForEach
        (
           (DynamicBuffer<DSPParametersElement> dspParamsBuffer, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer, ref GrainComponent grain) =>
           {
               for (int i = 0; i < dspParamsBuffer.Length; i++)
                   switch (dspParamsBuffer[i].DSPType)
                   {
                       case DSPTypes.Bitcrush:
                           DSP_Bitcrush.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                           break;
                       case DSPTypes.Delay:
                           break;
                       case DSPTypes.Flange:
                           DSP_Flange.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                           break;
                       case DSPTypes.Filter:
                           DSP_Filter.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                           break;
                       case DSPTypes.Chopper:
                           DSP_Chopper.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                           break;
                   }
           }
        ).ScheduleParallel(processPingPongSamplesJob);
        dspEffectSampleJob.Complete();

        #endregion

        Dependency = dspEffectSampleJob;
    }



    #region HELPERS

    public static float ComputeEmitterParameter(ModulationComponent mod, float randomValue)
    {
        float parameterRange = Mathf.Abs(mod.Max - mod.Min);
        float random = mod.UsePerlin ? mod.PerlinValue * mod.Noise : randomValue * mod.Noise;
        return Mathf.Clamp(mod.Min + (mod.StartValue + random) * parameterRange, mod.Min, mod.Max);
    }

    public static float ComputeBurstParameter(ModulationComponent mod, float currentSample, float totalSamples, float randomValue)
    {
        float parameterRange = Mathf.Abs(mod.Max - mod.Min);
        float timeShaped = Mathf.Pow(currentSample / totalSamples, mod.Exponent);
        float burstPath = timeShaped * (mod.EndValue - mod.StartValue);
        float modulation = mod.Modulation * mod.Input * parameterRange;

        if (mod.FixedStart) modulation *= timeShaped;
        else if (mod.FixedEnd) modulation *= 1 - timeShaped;

        float random = randomValue * mod.Noise * parameterRange;
        return Mathf.Clamp(mod.StartValue + burstPath + modulation + random, mod.Min, mod.Max);
    }

    public static float FadeFactor(int currentIndex, int fadeStart, int fadeEnd)
    {
        if (fadeStart == int.MaxValue || fadeEnd == int.MaxValue || fadeEnd == 0)
            return 1;

        return 1 - Mathf.Clamp((float)(currentIndex - fadeStart) / fadeEnd, 0, 1);
    }

    #endregion
}
