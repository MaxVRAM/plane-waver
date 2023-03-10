using System;
using UnityEngine;
using Unity.Entities;
using Random = UnityEngine.Random;

using MaxVRAM.GUI;

namespace PlaneWaver.Modulation
{
    public partial class Parameter
    {
        [Serializable]
        public class ModulationDataObject
        {
            #region DEFINITIONS

            private int _parameterIndex;
            public Vector2 ParameterRange;
            public Vector2 InitialRange;
            public bool ReversePath;
            public bool ModulationEnabled;
            public Vector2 ModInputRange;
            public float ModInputMultiplier;
            public bool Accumulate;
            public float Smoothing;
            public float ModExponent;
            public float ModInfluence;
            public bool FixedStart;
            public bool FixedEnd;
            public ModulationLimiter LimiterMode;
            public float NoiseInfluence;
            public float NoiseMultiplier;
            public float NoiseSpeed;
            public bool UsePerlin;
            public bool LockNoise;
            public float PerlinOffset { get; private set; }
            public float PerlinSeed { get; private set; }
            public float InitialValue { get; private set; }
            public bool VolatileEmitter { get; private set; }

            #endregion

            #region CONSTRUCTOR AND INITIALISATION

            public ModulationDataObject(PropertiesObject propertiesObject, bool isVolatileEmitter = false)
            {
                _parameterIndex = propertiesObject.Index;
                ParameterRange = propertiesObject.ParameterRange;
                InitialRange = propertiesObject.InitialRange;
                InitialValue = 0;
                ReversePath = propertiesObject.ReversePath;
                ModulationEnabled = false;
                ModInputRange = new Vector2(0f, 1f);
                ModInputMultiplier = 1;
                Accumulate = false;
                Smoothing = 0.2f;
                ModExponent = 1;
                ModInfluence = 0;
                FixedStart = propertiesObject.FixedStart;
                FixedEnd = propertiesObject.FixedEnd;
                LimiterMode = ModulationLimiter.Clip;
                NoiseInfluence = 0;
                NoiseMultiplier = 1;
                NoiseSpeed = 1;
                UsePerlin = isVolatileEmitter;
                LockNoise = false;
                PerlinOffset = 0;
                PerlinSeed = 0;
                VolatileEmitter = isVolatileEmitter;
            }

            public void Initialise()
            {
                if (VolatileEmitter) return;

                InitialValue = Random.Range(InitialRange.x, InitialRange.y);
                PerlinOffset = Random.Range(0f, 1000f) * (1 + _parameterIndex);
                PerlinSeed = Mathf.PerlinNoise(PerlinOffset + _parameterIndex, PerlinOffset * 0.5f + _parameterIndex);
            }
            
            public ModulationComponent BuildComponent(float modulationValue)
            {
                return new ModulationComponent {
                    StartValue = VolatileEmitter ? ReversePath ? InitialRange.y : InitialRange.x : InitialValue,
                    EndValue = VolatileEmitter ? ReversePath ? InitialRange.x : InitialRange.y  : 0,
                    ModValue = modulationValue,
                    ModInfluence = ModInfluence,
                    ModExponent = ModExponent,
                    Min = ParameterRange.x,
                    Max = ParameterRange.y,
                    Noise = NoiseInfluence * NoiseMultiplier,
                    PerlinValue = !VolatileEmitter && UsePerlin ? GetPerlinValue() : 0,
                    UsePerlin = !VolatileEmitter && UsePerlin,
                    LockNoise = LockNoise,
                    FixedStart = VolatileEmitter && FixedStart,
                    FixedEnd = VolatileEmitter && FixedEnd
                };
            }

            public float GetPerlinValue()
            {
                if (VolatileEmitter || !UsePerlin || Mathf.Approximately(NoiseInfluence, 0f))
                    return 0;

                PerlinOffset += NoiseSpeed * Time.deltaTime;
                return Mathf.PerlinNoise(PerlinSeed + PerlinOffset, (PerlinSeed + PerlinOffset) * 0.5f);
            }

            #endregion
        }
    }

    #region COMPONENT DATA MODEL
        
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
        
    #endregion
}