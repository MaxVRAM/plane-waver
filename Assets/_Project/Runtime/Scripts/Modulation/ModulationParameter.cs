
using UnityEngine;
using System;
using Unity.Entities;


namespace PlaneWaver
{
    [Serializable]
    public struct ModulationParameter
    {
        // TODO - Turn this into a DOTS struct component
        public Vector2 InputRange;
        public float InputMultiplier;
        public bool Accumulate;
        public float PreSmoothed;
        public float Smoothing;
        public float ModulationExponent;
        public float ModulationAmount;
        public ValueLimiter LimiterMode;
        public readonly Vector2 OutputRange;
        public bool ForCollisionEmitter;

        public ModulationParameter(Vector2 outputRange, bool forCollisionEmitter = false)
        {
            InputRange = new Vector2(0f,1f);
            InputMultiplier = 1;
            Smoothing = 0.2f;
            PreSmoothed = 0;
            ModulationExponent = 1;
            ModulationAmount = 0;
            Accumulate = false;
            LimiterMode = ValueLimiter.Clip;
            OutputRange = outputRange;
            ForCollisionEmitter = forCollisionEmitter;
        }
    }
    
    public struct ModComponent : IComponentData
    {
        public float StartValue;
        public float EndValue;
        public float Noise;
        public bool PerlinNoise;
        public bool LockNoise;
        public float PerlinValue;
        public float Exponent;
        public float Modulation;
        public float Min;
        public float Max;
        public bool FixedStart;
        public bool FixedEnd;
        public float Input;
    }

}