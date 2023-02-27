
using UnityEngine;
using System;

using MaxVRAM.Noise;

using Unity.Entities;
using Random = UnityEngine.Random;


namespace PlaneWaver.Modulation
{
    [Serializable]
    public struct Parameter
    {
        #region DEFINITIONS
        
        public int Index;
        public readonly Vector2 ParameterRange;
        public Vector2 StaticRange;
        public Vector2 InputRange;
        public float InputMultiplier;
        public bool Accumulate;
        public float Smoothing;
        public float ModulationExponent;
        public float ModulationInfluence;
        public bool FixedStart;
        public bool FixedEnd;
        public ValueLimiter LimiterMode;
        public float NoiseInfluence;
        public float NoiseMultiplier;
        public float NoiseSpeed;
        public bool UsePerlin;
        public bool LockNoise;
        public bool VolatileEmitter { get; private set; }
        public float OutputOffset { get; private set; }
        public float PreviousValue { get; set; }
        public float PerlinOffset { get; private set; }
        
        #endregion
        
        #region CONSTRUCTORS

        public Parameter(ParamDefaults defaults, bool volatileEmitter = false)
        {
            Index = defaults.Index;
            ParameterRange = defaults.Range;
            StaticRange = new Vector2(0f,1f);
            InputRange = new Vector2(0f,1f);
            InputMultiplier = 1;
            Accumulate = false;
            Smoothing = 0.2f;
            ModulationExponent = 1;
            ModulationInfluence = 0;
            FixedStart = volatileEmitter && defaults.FixedStart;
            FixedEnd =  volatileEmitter && defaults.FixedEnd;
            LimiterMode = ValueLimiter.Clip;
            NoiseInfluence = 0;
            NoiseMultiplier = 1;
            NoiseSpeed = 1;
            UsePerlin = false;
            LockNoise = false;
            VolatileEmitter = volatileEmitter;
            OutputOffset = 0;
            PreviousValue = 0;
            PerlinOffset = 0;
        }
        
        #endregion
        
        public void Initialise(bool volatileEmitter = false)
        {
            VolatileEmitter = volatileEmitter;
            if (VolatileEmitter) return;
            OutputOffset = Random.Range(InputRange.x, InputRange.y);
        }
        
        public ModComponent BuildModulationComponent(float perlinValue = 0)
        {
            return new ModComponent
            {
                StartValue = StaticRange.x,
                EndValue = StaticRange.y,
                ModValue = OutputOffset,
                ModInfluence = ModulationInfluence,
                ModExponent = ModulationExponent,
                Min = ParameterRange.x,
                Max = ParameterRange.y,
                Noise = NoiseInfluence * NoiseMultiplier,
                PerlinValue = perlinValue,
                UsePerlin = UsePerlin,
                LockNoise = LockNoise,
                FixedStart = FixedStart,
                FixedEnd = FixedEnd
            };
        }
        
        public ModComponent BuildModulationComponent(in PerlinNoise.Array noiseArray)
        {
            return BuildModulationComponent(GetPerlinValue(in noiseArray));
        }
        
        public float GetPerlinValue(in PerlinNoise.Array noiseArray)
        {
            if (VolatileEmitter || !UsePerlin || Mathf.Approximately(NoiseInfluence, 0f))
                return 0;
            
            PerlinOffset += NoiseSpeed * Time.deltaTime;
            return noiseArray.ValueAtOffset(Index, PerlinOffset) * NoiseMultiplier;
        }
    }

    public struct ModComponent : IComponentData
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