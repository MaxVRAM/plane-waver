using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;

using MaxVRAM;
using PlaneWaver.DSP;
using PlaneWaver.Emitters;
using PlaneWaver.Modulation;
using Random = Unity.Mathematics.Random;

namespace PlaneWaver
{
[UpdateInGroup(typeof(InitializationSystemGroup))]
class RandomSystem : ComponentSystem
{
    public NativeArray<Unity.Mathematics.Random> RandomArray { get; private set; }
    protected override void OnCreate()
    {
        var randomArray = new Unity.Mathematics.Random[JobsUtility.MaxJobThreadCount];
        var seed = new System.Random();

        for (var i = 0; i < JobsUtility.MaxJobThreadCount; ++i)
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
    private EndSimulationEntityCommandBufferSystem _commandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var windowing = GetSingleton<WindowingComponent>();
        var timer = GetSingleton<TimerComponent>();
        
        var samplesPerMS = (int)(timer.SampleRate * .001f);

        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter ecb = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        
        NativeArray<AssetSampleArray> audioClipData = GetEntityQuery(typeof(AssetSampleArray)).
            ToComponentDataArray<AssetSampleArray>(Allocator.TempJob);

        NativeArray<Random> randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;

        #region EMIT STABLE GRAINS
        
        JobHandle emitStableGrainsJob = Entities.WithName("EmitStableGrains")
            .WithNativeDisableParallelForRestriction(randomArray).WithReadOnly(audioClipData)
            .WithAll<EmitterReadyTag>().WithNone<EmitterVolatileTag>().ForEach
        ((
                int nativeThreadIndex, int entityInQueryIndex, ref EmitterComponent emitter,
                ref DynamicBuffer<AudioEffectParameters> dspChain) => 
            {
                // Max grains to stop it getting stuck in a while loop
                const int maxGrains = 50;
                var grainCount = 0;
                var dspTailLength = 0;

                // Get new random values
                Random randomGen = randomArray[nativeThreadIndex];
                float randomDensity = randomGen.NextFloat(-1, 1);
                float randomDuration = randomGen.NextFloat(-1, 1);
                float randomPlayhead = randomGen.NextFloat(-1, 1);
                float randomTranspose = randomGen.NextFloat(-1, 1);
                float randomVolume = randomGen.NextFloat(-1, 1);

                // Compute first grain value
                float density = ComputeEmitterParameter(emitter.ModDensity, randomDensity);
                int duration = (int)ComputeEmitterParameter(emitter.ModDuration, randomDuration) * samplesPerMS;
                var offset = 0;
                int sampleIndexNextGrainStart = timer.NextFrameIndexEstimate;
                if (emitter is { LastSampleIndex: > 0, LastGrainDuration: > 0 })
                {
                    offset = (int)(emitter.LastGrainDuration / density);
                    sampleIndexNextGrainStart = emitter.LastSampleIndex + offset;
                }
                float playhead = ComputeEmitterParameter(emitter.ModPlayhead, randomPlayhead);
                float transpose = ComputeEmitterParameter(emitter.ModTranspose, randomTranspose);
                float pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));

                float fadeFactor = 1;
                // float fadeFactor = FadeFactor(sampleIndexNextGrainStart - timer.NextFrameIndexEstimate,
                //     emitter.SamplesUntilFade, emitter.SamplesUntilDeath);
                float volume = ComputeEmitterParameter(emitter.ModVolume, randomVolume) * 
                            emitter.EmitterVolume * emitter.DynamicAmplitude * fadeFactor;

                // Create new grain
                while (sampleIndexNextGrainStart <= timer.NextFrameIndexEstimate + timer.GrainQueueSampleDuration &&
                       grainCount < maxGrains)
                {
                    if (volume > 0.005f)
                    {
                        // Prevent infinite loop if there's too many grains for some reason
                        grainCount++;
                        // Find the largest delay DSP effect tail in the chain so that the tail can be added to the sample and DSP buffers
                        for (var j = 0; j < dspChain.Length; j++)
                            if (dspChain[j].DelayBasedEffect)
                                if (dspChain[j].SampleTail > dspTailLength)
                                    dspTailLength = dspChain[j].SampleTail;

                        dspTailLength = Mathf.Clamp(dspTailLength, 0, timer.SampleRate - duration);
                        Entity grainProcessorEntity = ecb.CreateEntity(entityInQueryIndex);
                        
                        // Add ping-pong tag if needed
                        int clipLength = audioClipData[emitter.AudioClipIndex].SampleBlob.Value.Array.Length;
                        if (emitter.ReflectPlayhead && playhead * clipLength + duration * pitch >= clipLength)
                            ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new ReflectPlayheadTag());
                        
                        ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new GrainComponent
                        {
                            AssetSampleArray = audioClipData[emitter.AudioClipIndex],
                            PlayheadNorm = playhead,
                            SampleCount = duration,
                            Pitch = pitch,
                            Volume = volume,
                            SpeakerIndex = emitter.SpeakerIndex,
                            StartSampleIndex = sampleIndexNextGrainStart,
                            EffectTailSampleLength = dspTailLength
                        });
                        
                        ecb.AddBuffer<GrainSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        ecb.AddBuffer<DSPSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        
                        DynamicBuffer<AudioEffectParameters> dspParameters = ecb.AddBuffer<AudioEffectParameters>(entityInQueryIndex, grainProcessorEntity);
                        
                        foreach (AudioEffectParameters effect in dspChain)
                        {
                            AudioEffectParameters tempParams = effect;
                            tempParams.SampleStartTime = sampleIndexNextGrainStart;
                            dspParameters.Add(tempParams);
                        }
                    }
                    // Remember this grain's timing values for next frame
                    emitter.LastSampleIndex = sampleIndexNextGrainStart;
                    emitter.LastGrainDuration = duration;
                    // Get random values for next iteration and update random array to avoid repeating values
                    randomPlayhead = randomGen.NextFloat(-1, 1);
                    randomVolume = randomGen.NextFloat(-1, 1);
                    randomTranspose = randomGen.NextFloat(-1, 1);
                    randomDuration = randomGen.NextFloat(-1, 1);
                    randomDensity = randomGen.NextFloat(-1, 1);
                    randomArray[nativeThreadIndex] = randomGen;
                    // Compute grain values for next iteration
                    duration = (int)ComputeEmitterParameter(emitter.ModDuration, randomDuration) * samplesPerMS;
                    density = ComputeEmitterParameter(emitter.ModDensity, randomDensity);
                    offset = (int)(duration / density);
                    sampleIndexNextGrainStart += offset;
                    playhead = ComputeEmitterParameter(emitter.ModPlayhead, randomPlayhead);
                    transpose = ComputeEmitterParameter(emitter.ModTranspose, randomTranspose);
                    pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));

                    fadeFactor = FadeFactor(sampleIndexNextGrainStart - timer.NextFrameIndexEstimate, emitter.SamplesUntilFade, emitter.SamplesUntilDeath);
                    volume = ComputeEmitterParameter(emitter.ModVolume, randomVolume) * emitter.EmitterVolume * emitter.DynamicAmplitude * fadeFactor;
                }
            }
        ).ScheduleParallel(Dependency);
        emitStableGrainsJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(emitStableGrainsJob);

        #endregion

        #region EMIT VOLATILE GRAINS

        JobHandle emitVolatileGrainsJob = Entities.WithName("EmitVolatileGrains")
            .WithNativeDisableParallelForRestriction(randomArray).WithReadOnly(audioClipData)
            .WithAll<EmitterReadyTag, EmitterVolatileTag>().ForEach
        (
            (int nativeThreadIndex, int entityInQueryIndex, Entity entity, ref DynamicBuffer<AudioEffectParameters> dspChain, in EmitterComponent emitter) => 
            {
                var dspTailLength = 0;
                Random randomGen = randomArray[nativeThreadIndex];
                int currentDSPTime = timer.NextFrameIndexEstimate + (int)(randomGen.NextFloat(0, 1) * timer.RandomiseBurstStartIndex);

                var lengthRandom = (int)(randomGen.NextFloat(-1, 1) * emitter.ModLength.Noise * (emitter.ModLength.Max - emitter.ModLength.Min));
                int totalBurstLength = (int)Mathf.Clamp(emitter.ModLength.StartValue + emitter.ModLength.ModValue + lengthRandom,
                    emitter.ModLength.Min, emitter.ModLength.Max) * samplesPerMS;

                float randomDensity = randomGen.NextFloat(-1, 1);
                float randomDuration = randomGen.NextFloat(-1, 1);
                float randomPlayhead = randomGen.NextFloat(-1, 1);
                float randomTranspose = randomGen.NextFloat(-1, 1);
                float randomVolume = randomGen.NextFloat(-1, 1);

                // Compute first grain value
                var offset = 0;
                float density = ComputeBurstParameter(emitter.ModDensity, offset, totalBurstLength, randomDensity);
                int duration = (int)ComputeBurstParameter(emitter.ModDuration, offset, totalBurstLength, randomDuration) * samplesPerMS;

                float playhead = ComputeBurstParameter(emitter.ModPlayhead, offset, totalBurstLength, randomPlayhead);
                float transpose = ComputeBurstParameter(emitter.ModTranspose, offset, totalBurstLength, randomTranspose);
                float pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));

                float volume = ComputeBurstParameter(emitter.ModVolume, offset, totalBurstLength, randomVolume) * emitter.EmitterVolume * emitter.DynamicAmplitude;

                while (offset < totalBurstLength)
                {
                    if (volume > 0.005f)
                    {
                        // Find the largest delay DSP effect tail in the chain so that the tail can be added to the sample and DSP buffers
                        for (var j = 0; j < dspChain.Length; j++)
                            if (dspChain[j].DelayBasedEffect)
                                if (dspChain[j].SampleTail > dspTailLength)
                                    dspTailLength = dspChain[j].SampleTail;

                        dspTailLength = Mathf.Clamp(dspTailLength, 0, timer.SampleRate - duration);
                        Entity grainProcessorEntity = ecb.CreateEntity(entityInQueryIndex);

                        //Add ping-pong tag if needed
                        int clipLength = audioClipData[emitter.AudioClipIndex].SampleBlob.Value.Array.Length;
                        if (emitter.ReflectPlayhead && playhead * clipLength + duration * pitch >= clipLength)
                            ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new ReflectPlayheadTag());

                        ecb.AddComponent(entityInQueryIndex, grainProcessorEntity, new GrainComponent
                        {
                            AssetSampleArray = audioClipData[emitter.AudioClipIndex],
                            PlayheadNorm = playhead,
                            SampleCount = duration,
                            Pitch = pitch,
                            Volume = volume,
                            SpeakerIndex = emitter.SpeakerIndex,
                            StartSampleIndex = currentDSPTime + offset,
                            EffectTailSampleLength = dspTailLength
                        });
                        // Attach sample and DSP buffers to grain processor
                        ecb.AddBuffer<GrainSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        ecb.AddBuffer<DSPSampleBufferElement>(entityInQueryIndex, grainProcessorEntity);
                        // Add DSP parameters to grain processor
                        DynamicBuffer<AudioEffectParameters> dspParameters = ecb.AddBuffer<AudioEffectParameters>(entityInQueryIndex, grainProcessorEntity);

                        foreach (AudioEffectParameters effect in dspChain)
                        {
                            AudioEffectParameters tempParams = effect;
                            tempParams.SampleStartTime = currentDSPTime + offset;
                            dspParameters.Add(tempParams);
                        }
                    }
                    // Get random values for next iteration and update random array to avoid repeating values
                    if (!emitter.ModVolume.LockNoise)
                        randomVolume = randomGen.NextFloat(-1, 1);
                    if (!emitter.ModDensity.LockNoise)
                        randomDensity = randomGen.NextFloat(-1, 1);
                    if (!emitter.ModPlayhead.LockNoise)
                        randomPlayhead = randomGen.NextFloat(-1, 1);
                    if (!emitter.ModDuration.LockNoise)
                        randomDuration = randomGen.NextFloat(-1, 1);
                    if (!emitter.ModTranspose.LockNoise)
                        randomTranspose = randomGen.NextFloat(-1, 1);
                    randomArray[nativeThreadIndex] = randomGen;
                    // Compute grain values for next iteration
                    offset += (int)(duration / density);
                    density = ComputeBurstParameter(emitter.ModDensity, offset, totalBurstLength, randomDensity);
                    duration = (int)ComputeBurstParameter(emitter.ModDuration, offset, totalBurstLength, randomDuration) * samplesPerMS;
                    playhead = ComputeBurstParameter(emitter.ModPlayhead, offset, totalBurstLength, randomPlayhead);
                    transpose = ComputeBurstParameter(emitter.ModTranspose, offset, totalBurstLength, randomTranspose);
                    pitch = Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));
                    volume = ComputeBurstParameter(emitter.ModVolume, offset, totalBurstLength, randomVolume) * emitter.EmitterVolume * emitter.DynamicAmplitude;
                }
                ecb.RemoveComponent<EmitterReadyTag>(entityInQueryIndex, entity);
            }
        ).WithDisposeOnCompletion(audioClipData)
        .ScheduleParallel(emitStableGrainsJob);
        emitVolatileGrainsJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(emitVolatileGrainsJob);

        #endregion

        #region POPULATE GRAINS
        
        JobHandle processGrainSamplesJob = Entities.WithName("ProcessGrainSamples")
            .WithNone<ReflectPlayheadTag, SamplesProcessedTag>().ForEach
        (
            (int entityInQueryIndex, Entity entity, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer,
            DynamicBuffer<DSPSampleBufferElement> dspBuffer, in GrainComponent grain) =>
            {
                ref BlobArray<float> clipArray = ref grain.AssetSampleArray.SampleBlob.Value.Array;
                float sourceIndex = grain.PlayheadNorm * clipArray.Length;
                float increment = grain.Pitch;

                for (var i = 0; i < grain.SampleCount - 1; i++)
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

                    sourceValue = sourceIndexRemainder != 0
                            ? math.lerp(clipArray[(int)sourceIndex], clipArray[(int)sourceIndex + 1], sourceIndexRemainder)
                            : clipArray[(int)sourceIndex];
                    // Adjusted for volume and windowing
                    sourceValue *= grain.Volume;
                    sourceValue *= windowing.WindowingArray.Value.Array[
                        (int)MaxMath.Map(i, 0, grain.SampleCount, 0, windowing.WindowingArray.Value.Array.Length)];
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
        _commandBufferSystem.AddJobHandleForProducer(processGrainSamplesJob);

        #endregion

        #region POPULATE PiNG PONG GRAINS
        //-----------------------------------GRAIN PROCESSOR UPDATE
        //---TAKES GRAIN PROCESSOR INFORMATION AND FILLS THE SAMPLE BUFFER +DSP BUFFER(W / 0s TO THE SAME LENGTH AS SAMPLE BUFFER)
        JobHandle processPingPongSamplesJob = Entities.WithName("ProcessPingPongSamples")
            .WithAll<ReflectPlayheadTag>().WithNone<SamplesProcessedTag>().ForEach
        (
            (int entityInQueryIndex, Entity entity, DynamicBuffer <GrainSampleBufferElement> sampleOutputBuffer,
                DynamicBuffer<DSPSampleBufferElement> dspBuffer, in GrainComponent grain) =>
            {
                ref BlobArray<float> clipArray = ref grain.AssetSampleArray.SampleBlob.Value.Array;
                float sourceIndex = grain.PlayheadNorm * clipArray.Length;
                float increment = grain.Pitch;

                for (var i = 0; i < grain.SampleCount; i++)
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
                    sourceValue *= windowing.WindowingArray.Value.Array[
                        (int)MaxMath.Map(i, 0, grain.SampleCount, 0, windowing.WindowingArray.Value.Array.Length)];
                    sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = sourceValue });
                    dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                }

                // --Add additional samples to increase grain playback size based on DSP effect tail length
                for (var i = 0; i < grain.EffectTailSampleLength; i++)
                {
                    sampleOutputBuffer.Add(new GrainSampleBufferElement { Value = 0 });
                    dspBuffer.Add(new DSPSampleBufferElement { Value = 0 });
                }
                ecb.AddComponent(entityInQueryIndex, entity, new SamplesProcessedTag());
            }
        ).ScheduleParallel(emitVolatileGrainsJob);
        processPingPongSamplesJob.Complete();
        _commandBufferSystem.AddJobHandleForProducer(processPingPongSamplesJob);

        #endregion

        #region DSP CHAIN        

        JobHandle dspEffectSampleJob = Entities.WithName("DSPEffectSampleJob").WithAll<SamplesProcessedTag>().ForEach
        (
           (DynamicBuffer<AudioEffectParameters> dspParamsBuffer, DynamicBuffer<GrainSampleBufferElement> sampleOutputBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer, ref GrainComponent grain) =>
           {
               for (var i = 0; i < dspParamsBuffer.Length; i++)
                   switch (dspParamsBuffer[i].AudioEffectType)
                   {
                       case AudioEffectTypes.Bitcrush:
                           DSPBitcrush.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                           break;
                       case AudioEffectTypes.Delay:
                           break;
                       case AudioEffectTypes.Flange:
                           DSPFlange.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                           break;
                       case AudioEffectTypes.Filter:
                           DSPFilter.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                           break;
                       case AudioEffectTypes.Chopper:
                           DSPChopper.ProcessDSP(dspParamsBuffer[i], sampleOutputBuffer, dspBuffer);
                           break;
                   }
           }
        ).ScheduleParallel(processPingPongSamplesJob);
        dspEffectSampleJob.Complete();

        #endregion

        Dependency = dspEffectSampleJob;
    }



    #region HELPERS

    private static float ComputeEmitterParameter(ParameterComponent mod, float randomValue)
    {
        float random = (mod.UsePerlin ? mod.PerlinValue : randomValue) * mod.Noise * Mathf.Abs(mod.Max - mod.Min);
        //return Mathf.Clamp(mod.Min + (mod.StartValue + random) * parameterRange, mod.Min, mod.Max);
        return Mathf.Clamp(mod.StartValue + random, mod.Min, mod.Max);
    }

    private static float ComputeBurstParameter(ParameterComponent mod, float currentSample, float totalSamples, float randomValue)
    {
        float timeShaped = Mathf.Pow(currentSample / totalSamples, mod.TimeExponent);
        float burstPath = timeShaped * (mod.EndValue - mod.StartValue);
        float modulation = mod.ModValue;

        if (!mod.ModulateStart) modulation *= timeShaped;
        if (!mod.ModulateEnd) modulation *= 1 - timeShaped;

        float random = randomValue * mod.Noise * Mathf.Abs(mod.Max - mod.Min);
        return Mathf.Clamp(mod.StartValue + burstPath + modulation + random, mod.Min, mod.Max);
    }

    private static float FadeFactor(int currentIndex, int fadeStart, int fadeEnd)
    {
        if (fadeStart == int.MaxValue || fadeEnd is int.MaxValue or 0)
            return 1;

        return 1 - Mathf.Clamp((float)(currentIndex - fadeStart) / fadeEnd, 0, 1);
    }

    #endregion
}
}