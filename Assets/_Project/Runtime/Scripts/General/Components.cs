using Unity.Entities;
using Unity.Mathematics;

namespace PlaneWaver
{
    public struct TimerComponent : IComponentData
    {
        public int NextFrameIndexEstimate;
        public int GrainQueueSampleDuration;
        public int PreviousFrameSampleDuration;
        public int RandomiseBurstStartIndex;
        public int AverageGrainAge;
        public int SampleRate;
    }

    public struct ConnectionComponent : IComponentData
    {
        public float DeltaTime;
        public float ArcDegrees;
        public float ListenerRadius;
        public float BusyLoadLimit;
        public float SpeakerLingerTime;
        public float TranslationSmoothing;
        public float3 DisconnectedPosition;
        public float3 ListenerPos;
    }
    
    public struct SpeakerIndex : IComponentData
    {
        public int Value;
    }

    public enum ConnectionState
    {
        Pooled, Active, Lingering
    }

    public struct SpeakerComponent : IComponentData
    {
        public ConnectionState State;
        public int ConnectedHostCount;
        public float Radius;
        public float InactiveDuration;
        public float GrainLoad;
        public float3 Position;
    }
}