using PlaneWaver.Modulation;
using Unity.Entities;

namespace PlaneWaver.Emitters
{
    public enum PropagateCondition
    {
        Constant, Contact, Airborne, Collision
    }

    public struct EmitterComponent : IComponentData
    {
        public int SpeakerIndex;
        public int AudioClipIndex;
        public int LastSampleIndex;
        public int LastGrainDuration;
        public int SamplesUntilFade;
        public int SamplesUntilDeath;
        public bool ReflectPlayhead;
        public float EmitterVolume;
        public float DynamicAmplitude;
        public ModulationComponent ModVolume;
        public ModulationComponent ModPlayhead;
        public ModulationComponent ModDuration;
        public ModulationComponent ModDensity;
        public ModulationComponent ModTranspose;
        public ModulationComponent ModLength;
    }

    public struct EmitterReadyTag : IComponentData { }

    public struct EmitterVolatileTag : IComponentData { }
}