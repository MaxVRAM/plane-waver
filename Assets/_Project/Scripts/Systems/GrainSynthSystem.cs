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
        NativeArray<AudioClipDataComponent> audioClipData =
            GetEntityQuery(typeof(AudioClipDataComponent)).
            ToComponentDataArray<AudioClipDataComponent>
            (Allocator.TempJob);

        WindowingDataComponent windowingData = GetSingleton<WindowingDataComponent>();
        AudioTimerComponent dspTimer = GetSingleton<AudioTimerComponent>();
        var randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;

        #region EMIT GRAINS
        //---   CREATES ENTITIES W/ GRAIN PROCESSOR + GRAIN SAMPLE BUFFER + DSP SAMPLE BUFFER + DSP PARAMS BUFFER
        JobHandle emitGrains = Entities.WithNativeDisableParallelForRestriction(randomArray).WithReadOnly(audioClipData).ForEach
        (
            (int nativeThreadIndex, int entityInQueryIndex, ref DynamicBuffer<DSPParametersElement> dspChain, ref ContinuousComponent emitter, 
            ref InListenerRadiusTag listenerRadius, ref PlayingTag playing, ref ConnectedTag connected) => 
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
                float density = ComputeEmitterParameter(emitter._Density, randomDensity);
                int duration = (int)ComputeEmitterParameter(emitter._Duration, randomDuration) * samplesPerMS;
                int offset = 0;
                int sampleIndexNextGrainStart = dspTimer._NextFrameIndexEstimate;
                if (emitter._LastSampleIndex > 0 && emitter._PreviousGrainDuration > 0)
                {
                    offset = (int)(emitter._PreviousGrainDuration / density);
                    sampleIndexNextGrainStart = emitter._LastSampleIndex + offset;
                }
                float playhead = ComputeEmitterParameter(emitter._Playhead, randomPlayhead);
                float transpose = ComputeEmitterParameter(emitter._Transpose, randomTranspose);
                float pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));

                float fadeFactor = FadeFactor(sampleIndexNextGrainStart - dspTimer._NextFrameIndexEstimate, emitter._SamplesUntilFade, emitter._SamplesUntilDeath);
                float volume = ComputeEmitterParameter(emitter._Volume, randomVolume) * emitter._DistanceAmplitude * emitter._VolumeAdjust * fadeFactor;

                // Create new grain
                while (sampleIndexNextGrainStart <= dspTimer._NextFrameIndexEstimate + dspTimer._GrainQueueSampleDuration && grainCount < maxGrains)
                {
                    if (volume > 0.005f)
                    {
                        // Prevent infinite loop if there's too many grains for some reason
                        grainCount++;
                        // Find the largest delay DSP effect tail in the chain so that the tail can be added to the sample and DSP buffers
                        for (int j = 0; j < dspChain.Length; j++)
                            if (dspChain[j]._DelayBasedEffect)
                                if (dspChain[j]._SampleTail > dspTailLength)
                                    dspTailLength = dspChain[j]._SampleTail;

                        dspTailLength = Mathf.Clamp(dspTailLength, 0, sampleRate - duration);
                        Entity grainProcessorEntity = ecb.CreateEntity(entityInQueryIndex);
                        // Add ping-pong tag if needed
                        int clipLength = audioClipData[emitter._AudioClipIndex]._ClipDataBlobAsset.Value.array.Length;
                        if (emitter._PingPong && playhead * clipLength + duration * pitch >= clipLength)
                            ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new PingPongTag());
                        // Build grain processor entity
                        ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new GrainComponent
                        {
                            _AudioClipDataComponent = audioClipData[emitter._AudioClipIndex],
                            _PlayheadNorm = playhead,
                            _SampleCount = duration,
                            _Pitch = pitch,
                            _Volume = volume,
                            _SpeakerIndex = emitter._SpeakerIndex,
                            _StartSampleIndex = sampleIndexNextGrainStart,
                            _EffectTailSampleLength = dspTailLength
                        });
                        // Attach sample and DSP buffers to grain processor
                        ecb.AddBuffer<GrainSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        ecb.AddBuffer<DSPSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        // Add DSP parameters to grain processor
                        DynamicBuffer<DSPParametersElement> dspParameters = ecb.AddBuffer<DSPParametersElement>(entityInQueryIndex, grainProcessorEntity);
                        for (int i = 0; i < dspChain.Length; i++)
                        {
                            DSPParametersElement tempParams = dspChain[i];
                            tempParams._SampleStartTime = sampleIndexNextGrainStart;
                            dspParameters.Add(tempParams);
                        }
                    }
                    // Remember this grain's timing values for next frame
                    emitter._LastSampleIndex = sampleIndexNextGrainStart;
                    emitter._PreviousGrainDuration = duration;
                    // Get random values for next iteration and update random array to avoid repeating values
                    randomPlayhead = randomGen.NextFloat(-1, 1);
                    randomVolume = randomGen.NextFloat(-1, 1);
                    randomTranspose = randomGen.NextFloat(-1, 1);
                    randomDuration = randomGen.NextFloat(-1, 1);
                    randomDensity = randomGen.NextFloat(-1, 1);
                    randomArray[nativeThreadIndex] = randomGen;
                    // Compute grain values for next iteration
                    duration = (int)ComputeEmitterParameter(emitter._Duration, randomDuration) * samplesPerMS;
                    density = ComputeEmitterParameter(emitter._Density, randomDensity);
                    offset = (int)(duration / density);
                    sampleIndexNextGrainStart += offset;
                    playhead = ComputeEmitterParameter(emitter._Playhead, randomPlayhead);
                    transpose = ComputeEmitterParameter(emitter._Transpose, randomTranspose);
                    pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));

                    fadeFactor = FadeFactor(sampleIndexNextGrainStart - dspTimer._NextFrameIndexEstimate, emitter._SamplesUntilFade, emitter._SamplesUntilDeath);
                    volume = ComputeEmitterParameter(emitter._Volume, randomVolume) * emitter._DistanceAmplitude * emitter._VolumeAdjust * fadeFactor;
                }
            }
        ).ScheduleParallel(Dependency);
        emitGrains.Complete();
        _CommandBufferSystem.AddJobHandleForProducer(emitGrains);

        #endregion

        #region BURST GRAINS

        JobHandle emitBurst = Entities.WithNativeDisableParallelForRestriction(randomArray).WithReadOnly(audioClipData).ForEach
        (
            (int nativeThreadIndex, int entityInQueryIndex, ref DynamicBuffer<DSPParametersElement> dspChain, ref BurstComponent burst, 
            ref InListenerRadiusTag listenerRadius, ref PlayingTag playing, ref ConnectedTag connected) => 
            {
                int grainsCreated = 0;
                int dspTailLength = 0;
                var randomGen = randomArray[nativeThreadIndex];
                int currentDSPTime = dspTimer._NextFrameIndexEstimate + (int)(randomGen.NextFloat(0, 1) * dspTimer._RandomiseBurstStartIndex);

                int lengthRange = (int)(burst._Length._Max - burst._Length._Min);

                int lengthModulation = (int)(MaxMath.Map(burst._Length._Input, 0, 1, 0, 1,
                    burst._Length._Exponent) * burst._Length._Modulation * lengthRange);

                int lengthRandom = (int)(randomGen.NextFloat(-1, 1) * burst._Length._Noise * lengthRange);

                int totalBurstLength = (int)Mathf.Clamp(burst._Length._StartValue + lengthModulation + lengthRandom,
                    burst._Length._Min, burst._Length._Max) * samplesPerMS;

                float randomDensity = randomGen.NextFloat(-1, 1);
                float randomDuration = randomGen.NextFloat(-1, 1);
                float randomPlayhead = randomGen.NextFloat(-1, 1);
                float randomTranspose = randomGen.NextFloat(-1, 1);
                float randomVolume = randomGen.NextFloat(-1, 1);

                // Compute first grain value
                int offset = 0;
                float density = ComputeBurstParameter(burst._Density, offset, totalBurstLength, randomDensity);
                int duration = (int)ComputeBurstParameter(burst._Duration, offset, totalBurstLength, randomDuration) * samplesPerMS;

                float playhead = ComputeBurstParameter(burst._Playhead, offset, totalBurstLength, randomPlayhead);
                float transpose = ComputeBurstParameter(burst._Transpose, offset, totalBurstLength, randomTranspose);
                float pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));

                float volume = ComputeBurstParameter(burst._Volume, offset, totalBurstLength, randomVolume) * burst._VolumeAdjust * burst._DistanceAmplitude;

                while (offset < totalBurstLength)
                {
                    if (volume > 0.005f)
                    {
                        grainsCreated++;
                        // Find the largest delay DSP effect tail in the chain so that the tail can be added to the sample and DSP buffers
                        for (int j = 0; j < dspChain.Length; j++)
                            if (dspChain[j]._DelayBasedEffect)
                                if (dspChain[j]._SampleTail > dspTailLength)
                                    dspTailLength = dspChain[j]._SampleTail;

                        dspTailLength = Mathf.Clamp(dspTailLength, 0, sampleRate - duration);
                        Entity grainProcessorEntity = ecb.CreateEntity(entityInQueryIndex);

                        //Add ping-pong tag if needed
                        int clipLength = audioClipData[burst._AudioClipIndex]._ClipDataBlobAsset.Value.array.Length;
                        if (burst._PingPong && playhead * clipLength + duration * pitch >= clipLength)
                            ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new PingPongTag());

                        ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new GrainComponent
                        {
                            _AudioClipDataComponent = audioClipData[burst._AudioClipIndex],
                            _PlayheadNorm = playhead,
                            _SampleCount = duration,
                            _Pitch = pitch,
                            _Volume = volume,
                            _SpeakerIndex = burst._SpeakerIndex,
                            _StartSampleIndex = currentDSPTime + offset,
                            _EffectTailSampleLength = dspTailLength
                        });
                        // Attach sample and DSP buffers to grain processor
                        ecb.AddBuffer<GrainSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        ecb.AddBuffer<DSPSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        // Add DSP parameters to grain processor
                        DynamicBuffer<DSPParametersElement> dspParameters = ecb.AddBuffer<DSPParametersElement>(entityInQueryIndex, grainProcessorEntity);

                        for (int i = 0; i < dspChain.Length; i++)
                        {
                            DSPParametersElement tempParams = dspChain[i];
                            tempParams._SampleStartTime = currentDSPTime + offset;
                            dspParameters.Add(tempParams);
                        }
                    }
                    // Get random values for next iteration and update random array to avoid repeating values
                    if (!burst._Density._LockNoise)
                        randomDensity = randomGen.NextFloat(-1, 1);
                    if (!burst._Playhead._LockNoise)
                        randomPlayhead = randomGen.NextFloat(-1, 1);
                    if (!burst._Duration._LockNoise)
                        randomDuration = randomGen.NextFloat(-1, 1);
                    randomVolume = randomGen.NextFloat(-1, 1);
                    if (!burst._Transpose._LockNoise)
                        randomTranspose = randomGen.NextFloat(-1, 1);
                    randomArray[nativeThreadIndex] = randomGen;
                    // Compute grain values for next iteration
                    offset += (int)(duration / density);
                    density = ComputeBurstParameter(burst._Density, offset, totalBurstLength, randomDensity);
                    duration = (int)ComputeBurstParameter(burst._Duration, offset, totalBurstLength, randomDuration) * samplesPerMS;
                    playhead = ComputeBurstParameter(burst._Playhead, offset, totalBurstLength, randomPlayhead);
                    transpose = ComputeBurstParameter(burst._Transpose, offset, totalBurstLength, randomTranspose);
                    pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));
                    volume = ComputeBurstParameter(burst._Volume, offset, totalBurstLength, randomVolume) * burst._VolumeAdjust * burst._DistanceAmplitude;
                }
                burst._IsPlaying = false;
            }
        ).WithDisposeOnCompletion(audioClipData)
        .ScheduleParallel(emitGrains);
        emitBurst.Complete();
        // Make sure that the ECB system knows about our job
        _CommandBufferSystem.AddJobHandleForProducer(emitBurst);

        #endregion

        #region POPULATE GRAINS
        // ----------------------------------- GRAIN PROCESSOR UPDATE
        //---   TAKES GRAIN PROCESSOR INFORMATION AND FILLS THE SAMPLE BUFFER + DSP BUFFER (W/ 0s TO THE SAME LENGTH AS SAMPLE BUFFER)
        //NativeArray<Entity> grainProcessorEntities = GetEntityQuery(typeof(GrainComponent)).ToEntityArray(Allocator.TempJob);
        JobHandle processGrains = Entities.WithNone<PingPongTag, SamplesProcessedTag>().ForEach
        (
            (int entityInQueryIndex, Entity entity, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer,
            DynamicBuffer<DSPSampleBufferElement> dspBuffer, in GrainComponent grain) =>
            {
                ref BlobArray<float> clipArray = ref grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array;
                float sourceIndex = grain._PlayheadNorm * clipArray.Length;
                float increment = grain._Pitch;

                // Suck up all the juicy samples from the source content
                for (int i = 0; i < grain._SampleCount - 1; i++)
                {
                    // Set rate of sample read to alter pitch - interpolate sample if not integer to create 
                    sourceIndex += increment;
                    float sourceIndexRemainder = sourceIndex % 1;
                    float sourceValue;

                    if (sourceIndex + 1 >= clipArray.Length) sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = 0 });
                    else
                    {
                        if (sourceIndexRemainder != 0)
                            sourceValue = math.lerp(clipArray[(int)sourceIndex], clipArray[(int)sourceIndex + 1], sourceIndexRemainder);
                        else
                            sourceValue = grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array[(int)sourceIndex];
                        // Adjusted for volume and windowing
                        sourceValue *= grain._Volume;
                        sourceValue *= windowingData._WindowingArray.Value.array[
                            (int)MaxMath.Map(i, 0, grain._SampleCount, 0, windowingData._WindowingArray.Value.array.Length)];
                        sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = sourceValue });
                    }
                    dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                }
                // --Add additional samples to increase grain playback size based on DSP effect tail length
                for (int i = 0; i < grain._EffectTailSampleLength; i++)
                {
                    sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = 0 });
                    dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                }
                ecb.AddComponent(entityInQueryIndex, entity, new SamplesProcessedTag());
            }
        ).ScheduleParallel(emitBurst);
        processGrains.Complete();
        #endregion

        #region POPULATE PiNG PONG GRAINS
        //-----------------------------------GRAIN PROCESSOR UPDATE
        //---TAKES GRAIN PROCESSOR INFORMATION AND FILLS THE SAMPLE BUFFER +DSP BUFFER(W / 0s TO THE SAME LENGTH AS SAMPLE BUFFER)
        JobHandle processPingPongGrains = Entities.WithNone<SamplesProcessedTag>().ForEach
        (
            (int entityInQueryIndex, Entity entity, DynamicBuffer <GrainSampleBufferElement> sampleOutputBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer, in GrainComponent grain, in PingPongTag pingpong) =>
            {
                int clipLength = grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array.Length;
                float sourceIndex = grain._PlayheadNorm * clipLength;
                float increment = grain._Pitch;

                for (int i = 0; i < grain._SampleCount; i++)
                {
                    // Ping pong samples at the limit of the sample
                    if (sourceIndex + increment < 0 || sourceIndex + increment > clipLength - 1)
                        increment = -increment;
                    // Set rate of sample read to alter pitch - interpolate sample if not integer to create 
                    sourceIndex += increment;
                    float sourceIndexRemainder = sourceIndex % 1;
                    float sourceValue;
                    if (sourceIndexRemainder != 0)
                        sourceValue = math.lerp(
                            grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array[(int)sourceIndex],
                            grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array[(int)sourceIndex + 1],
                            sourceIndexRemainder);
                    else sourceValue = grain._AudioClipDataComponent._ClipDataBlobAsset.Value.array[(int)sourceIndex];
                    // Adjusted for volume and windowing
                    sourceValue *= grain._Volume;
                    sourceValue *= windowingData._WindowingArray.Value.array[
                        (int)MaxMath.Map(i, 0, grain._SampleCount, 0, windowingData._WindowingArray.Value.array.Length)];
                    sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = sourceValue });
                    dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                }

                // --Add additional samples to increase grain playback size based on DSP effect tail length
                for (int i = 0; i < grain._EffectTailSampleLength; i++)
                {
                    sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = 0 });
                    dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                }
                //ecb.AddComponent(entityInQueryIndex, grainProcessorEntities[entityInQueryIndex], new SamplesProcessedTag());
                ecb.AddComponent(entityInQueryIndex, entity, new SamplesProcessedTag());
            }
        ).ScheduleParallel(emitBurst);

        processPingPongGrains.Complete();

        //).WithDisposeOnCompletion(grainProcessorEntities)

        #endregion

        #region DSP CHAIN        

        //JobHandle dspGrains = Entities.WithAll<SamplesProcessedTag>().ForEach
        //(
        //   (DynamicBuffer<DSPParametersElement> dspParamsBuffer, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, DynamicBuffer < DSPSampleBufferElement > dspBuffer, ref GrainComponent grain) =>
        //   {
        //        for (int i = 0; i < dspParamsBuffer.Length; i++)
        //            switch (dspParamsBuffer[i]._DSPType)
        //            {
        //                case DSPTypes.Bitcrush:
        //                    DSP_Bitcrush.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
        //                    break;
        //                case DSPTypes.Delay:
        //                    break;
        //                case DSPTypes.Flange:
        //                    DSP_Flange.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
        //                    break;
        //                case DSPTypes.Filter:
        //                    DSP_Filter.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
        //                    break;
        //                case DSPTypes.Chopper:
        //                    DSP_Chopper.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
        //                    break;
        //            }
        //   }
        //).ScheduleParallel(processPingPongGrains);
        //dspGrains.Complete();

        #endregion

        Dependency = processPingPongGrains;
    }



    #region HELPERS

    public static float ComputeEmitterParameter(ModulationComponent mod, float randomValue)
    {
        float parameterRange = Mathf.Abs(mod._Max - mod._Min);
        float random = mod._PerlinNoise ? mod._PerlinValue * mod._Noise : randomValue * mod._Noise;
        return Mathf.Clamp(mod._Min + (mod._StartValue + random) * parameterRange, mod._Min, mod._Max);
    }

    public static float ComputeBurstParameter(ModulationComponent mod, float currentSample, float totalSamples, float randomValue)
    {
        float parameterRange = Mathf.Abs(mod._Max - mod._Min);
        float timeShaped = Mathf.Pow(currentSample / totalSamples, mod._Exponent);
        float burstPath = timeShaped * (mod._EndValue - mod._StartValue);
        float modulation = mod._Modulation * mod._Input * parameterRange;

        if (mod._FixedStart) modulation *= timeShaped;
        else if (mod._FixedEnd) modulation *= 1 - timeShaped;

        float random = randomValue * mod._Noise * parameterRange;
        return Mathf.Clamp(mod._StartValue + burstPath + modulation + random, mod._Min, mod._Max);
    }

    public static float FadeFactor(int currentIndex, int fadeStart, int fadeEnd)
    {
        if (fadeStart == int.MaxValue || fadeEnd == int.MaxValue || fadeEnd == 0)
            return 1;

        return 1 - Mathf.Clamp((float)(currentIndex - fadeStart) / fadeEnd, 0, 1);
    }

    #endregion
}
