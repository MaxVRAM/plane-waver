using Unity.Entities;
using Unity.Mathematics;

namespace PlaneWaver
{
    public struct AudioTimerComponent : IComponentData
    {
        public int NextFrameIndexEstimate;
        public int GrainQueueSampleDuration;
        public int PreviousFrameSampleDuration;
        public int RandomiseBurstStartIndex;
        public int AverageGrainAge;
    }

    public struct ConnectionConfig : IComponentData
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
}