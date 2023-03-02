using System;
using Unity.Entities;

namespace PlaneWaver.DSP
{
    public struct FloatBlobAsset
    {
        public BlobArray<float> Array;
    }
    
    public struct SamplesProcessedTag : IComponentData { }

    public struct ReflectPlayheadTag : IComponentData { }
    
    public struct WindowingComponent : IComponentData
    {
        public BlobAssetReference<FloatBlobAsset> WindowingArray;
    }
    
    public struct GrainSampleBufferElement : IBufferElementData
    {
        public float Value;
    }

    public struct DSPSampleBufferElement : IBufferElementData
    {
        public float Value;
    }
    
    public struct AssetSampleArray :IComponentData
    {
        public int AssetIndex;
        public BlobAssetReference<FloatBlobAsset> SampleBlob;
    }
    
    public enum AudioEffectTypes
    {
        Bitcrush, Flange, Delay, Filter, Chopper
    }
    
    [Serializable]
    public struct AudioEffectParameters : IBufferElementData
    {
        public AudioEffectTypes AudioEffectType;
        public bool DelayBasedEffect;
        public int SampleRate;
        public int SampleTail;
        public int SampleStartTime;
        public float Mix;
        public float Value0;
        public float Value1;
        public float Value2;
        public float Value3;
        public float Value4;
        public float Value5;
        public float Value6;
        public float Value7;
        public float Value8;
        public float Value9;
        public float Value10;
    }
    
    public struct GrainComponent : IComponentData
    {
        public AssetSampleArray AssetSampleArray;
        public int StartSampleIndex;
        public int SampleCount;
        public float PlayheadNorm;
        public float Pitch;
        public float Volume;
        public int SpeakerIndex;
        public int EffectTailSampleLength;
    }
    
    #region GRAIN CLASS

    public class Grain
    {
        public bool Pooled = true;
        public bool IsPlaying = false;
        public float[] SampleData;
        public int PlayheadIndex = 0;
        public float PlayheadNormalised = 0;
        public int SizeInSamples = -1;
        public int DSPStartTime;

        public Grain(int maxGrainSize)
        {
            SampleData = new float[maxGrainSize];
        }
    }

    #endregion
}