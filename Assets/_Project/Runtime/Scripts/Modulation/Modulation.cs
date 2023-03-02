using System;
using Unity.Entities;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public partial class Modulation
    {

    }

    public struct ModulationComponent : IComponentData
    {
        public float StartValue;
        public float EndValue;
        public float ModValue;
        public float ModInfluence;
        public float ModExponent;
        public float Min;
        public float Max;
        public float Noise;
        public float PerlinValue;
        public bool UsePerlin;
        public bool LockNoise;
        public bool FixedStart;
        public bool FixedEnd;
    }
}