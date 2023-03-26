using System;
using UnityEngine;
using Unity.Entities;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class ModulationData
    {
        #region DEFINITIONS

        public bool Enabled;

        public string Name;
        public int ParameterIndex;
        public bool IsVolatileEmitter;
        
        public ModulationInput Input;

        public Vector2 ParameterRange;
        public Vector2 InitialRange;
        public bool ReversePath;

        public Vector2 ModInputRange;
        public bool Absolute;
        public float ModInputMultiplier;
        public bool Accumulate;
        public float Smoothing;
        public float InputExponent;
        public float ModInfluence;

        public float TimeExponent;
        public bool FixedStart;
        public bool FixedEnd;

        public ModulationLimiter LimiterMode;

        public bool NoiseEnabled;
        public float NoiseInfluence;
        public float NoiseMultiplier;
        public float PerlinSpeed;
        public bool UsePerlin;
        public bool LockNoise;

        #endregion

        #region CONSTRUCTOR AND INITIALISATION

        public ModulationData(in Parameter.Defaults defaults, bool isVolatileEmitter = false)
        {
            Name = defaults.Name;
            ParameterIndex = defaults.Index;
            IsVolatileEmitter = isVolatileEmitter;
            Input = new ModulationInput();
            
            ParameterRange = defaults.ParameterRange;
            InitialRange = defaults.InitialRange;
            ReversePath = defaults.ReversePath;
            Enabled = false;
            ModInputRange = new Vector2(0f, 1f);
            Absolute = false;
            ModInputMultiplier = 1;
            Accumulate = false;
            Smoothing = 0.2f;
            InputExponent = 1;
            TimeExponent = 1;
            ModInfluence = 0;
            FixedStart = defaults.FixedStart;
            FixedEnd = defaults.FixedEnd;
            LimiterMode = ModulationLimiter.Clip;
            NoiseEnabled = false;
            NoiseInfluence = 0;
            NoiseMultiplier = 0.1f;
            UsePerlin = !isVolatileEmitter;
            PerlinSpeed = 1;
            LockNoise = false;
        }
        
        public ModulationComponent BuildComponent(float modulationValue)
        {
            return IsVolatileEmitter ? BuildVolatileComponent(modulationValue) : BuildStableComponent(modulationValue);
        }

        public ModulationComponent BuildVolatileComponent(float modulationValue)
        {
            return new ModulationComponent {
                StartValue = ReversePath ? InitialRange.y : InitialRange.x,
                EndValue = ReversePath ? InitialRange.x : InitialRange.y,
                ModValue = modulationValue,
                Min = ParameterRange.x,
                Max = ParameterRange.y,
                Noise = NoiseEnabled ? NoiseInfluence * NoiseMultiplier : 0,
                PerlinValue = -1,
                UsePerlin = false,
                LockNoise = LockNoise,
                TimeExponent = TimeExponent,
                FixedStart = FixedStart,
                FixedEnd = FixedEnd
            };
        }

        public ModulationComponent BuildStableComponent(float modulationValue, float perlinValue = -1)
        {
            return new ModulationComponent {
                StartValue = modulationValue,
                EndValue = -1,
                ModValue = -1,
                Min = ParameterRange.x,
                Max = ParameterRange.y,
                Noise = NoiseEnabled ? NoiseInfluence * NoiseMultiplier : 0,
                PerlinValue = perlinValue,
                UsePerlin = UsePerlin,
                LockNoise = false,
                TimeExponent = -1,
                FixedStart = false,
                FixedEnd = false
            };
        }

        #endregion
    }

    #region COMPONENT DATA MODEL

    public struct ModulationComponent : IComponentData
    {
        public float StartValue;
        public float EndValue;
        public float TimeExponent;
        public bool FixedStart;
        public bool FixedEnd;
        public float ModValue;
        public float Min;
        public float Max;
        public float Noise;
        public float PerlinValue;
        public bool UsePerlin;
        public bool LockNoise;
    }

    #endregion
}