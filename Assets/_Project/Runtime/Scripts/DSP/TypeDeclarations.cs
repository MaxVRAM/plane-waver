using Unity.Entities;

namespace PlaneWaver.DSP
{
    public struct SamplesProcessedTag : IComponentData { }

    public struct ReflectPlayheadTag : IComponentData { }
    
    public struct WindowingDataComponent : IComponentData
    {
        public BlobAssetReference<FloatBlobAsset> WindowingArray;
    }
    
    public struct AssetSampleArray :IComponentData
    {
        public int AssetIndex;
        public BlobAssetReference<FloatBlobAsset> SampleBlob;
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