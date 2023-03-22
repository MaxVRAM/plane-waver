using System;
using UnityEngine;
using Unity.Entities;
using Random = UnityEngine.Random;

namespace PlaneWaver.Modulation
{
    public partial class Parameter
    {
        [Serializable]
        public class ModulationDataObject
        {
            #region DEFINITIONS

            private int _parameterIndex;
            public bool IsVolatileEmitter;
            public Vector2 ParameterRange;
            public Vector2 InitialRange;
            public bool ReversePath;
            public bool Enabled;
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
            public float PerlinOffset { get; private set; }
            public float PerlinSeed { get; private set; }
            public float InitialValue { get; set; }

            #endregion

            #region CONSTRUCTOR AND INITIALISATION

            public ModulationDataObject(PropertiesObject propertiesObject, bool isVolatileEmitter = false)
            {
                _parameterIndex = propertiesObject.Index;
                IsVolatileEmitter = isVolatileEmitter;
                ParameterRange = propertiesObject.ParameterRange;
                InitialRange = propertiesObject.InitialRange;
                InitialValue = 0;
                ReversePath = propertiesObject.ReversePath;
                Enabled = false;
                ModInputRange = new Vector2(0f, 1f);
                Absolute = false;
                ModInputMultiplier = 1;
                Accumulate = false;
                Smoothing = 0.2f;
                InputExponent = 1;
                TimeExponent = 1;
                ModInfluence = 0;
                FixedStart = propertiesObject.FixedStart;
                FixedEnd = propertiesObject.FixedEnd;
                LimiterMode = ModulationLimiter.Clip;
                NoiseEnabled = false;
                NoiseInfluence = 0;
                NoiseMultiplier = 0.1f;
                UsePerlin = !isVolatileEmitter;
                PerlinSpeed = 1;
                LockNoise = false;
                PerlinOffset = 0;
                PerlinSeed = 0;
            }

            public void Initialise()
            {
                if (IsVolatileEmitter) return;

                InitialValue = GetNewInitialValue();
                PerlinOffset = Random.Range(0f, 1000f) * (1 + _parameterIndex);
                PerlinSeed = Mathf.PerlinNoise(PerlinOffset + _parameterIndex, PerlinOffset * 0.5f + _parameterIndex);
            }
            
            public float GetNewInitialValue()
            {
                Random.InitState((int) (Time.realtimeSinceStartup * 1000));
                return Random.Range(InitialRange.x, InitialRange.y);
            }
            
            public ModulationComponent BuildComponent(float modulationValue)
            {
                return IsVolatileEmitter
                        ? BuildVolatileComponent(modulationValue)
                        : BuildStableComponent(modulationValue);
            }
            
            public ModulationComponent BuildVolatileComponent(float modulationValue)
            {
                return new ModulationComponent {
                    StartValue = ReversePath ? InitialRange.y : InitialRange.x,
                    EndValue = ReversePath ? InitialRange.x : InitialRange.y,
                    ModValue = modulationValue,
                    ModInfluence = ModInfluence,
                    InputExponent = InputExponent,
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
            
            public ModulationComponent BuildStableComponent(float modulationValue)
            {
                return new ModulationComponent {
                    StartValue = modulationValue,
                    EndValue = -1,
                    ModValue = -1,
                    ModInfluence = -1,
                    InputExponent = -1,
                    Min = ParameterRange.x,
                    Max = ParameterRange.y,
                    Noise = NoiseEnabled ? NoiseInfluence * NoiseMultiplier : 0,
                    PerlinValue = GetPerlinValue(),
                    UsePerlin = UsePerlin,
                    LockNoise = false,
                    TimeExponent = -1,
                    FixedStart = false,
                    FixedEnd = false
                };
            }

            public float GetPerlinValue()
            {
                if (IsVolatileEmitter || !NoiseEnabled || !UsePerlin || Mathf.Approximately(NoiseInfluence, 0f))
                    return 0;

                PerlinOffset += PerlinSpeed * Time.deltaTime;
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
        public float InputExponent;
        public float TimeExponent;
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