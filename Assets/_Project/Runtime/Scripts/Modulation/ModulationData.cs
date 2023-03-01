using System;
using UnityEngine;
using Random = UnityEngine.Random;

using PlaneWaver.Interaction;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public struct Data
    {
        #region DEFINITIONS

        public readonly int ParameterIndex;
        public readonly Vector2 ParameterRange;
        public Vector2 StaticRange;
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
        public float OutputOffset { get; private set; }
        public float PreviousValue { get; set; }
        public bool VolatileEmitter { get; private set; }

        public float OutputValue;
        private Source _inputSource;

        #endregion

        #region CONSTRUCTOR AND INITIALISATION

        public Data(Defaults defaultsValues, bool volatileEmitter = false)
        {
            ParameterIndex = defaultsValues.Index;
            ParameterRange = defaultsValues.Range;
            StaticRange = new Vector2(0f, 1f);
            ModInputRange = new Vector2(0f, 1f);
            ModInputMultiplier = 1;
            Accumulate = false;
            Smoothing = 0.2f;
            ModExponent = 1;
            ModInfluence = 0;
            FixedStart = defaultsValues.FixedStart;
            FixedEnd = defaultsValues.FixedEnd;
            LimiterMode = ModulationLimiter.Clip;
            NoiseInfluence = 0;
            NoiseMultiplier = 1;
            NoiseSpeed = 1;
            UsePerlin = false;
            LockNoise = false;
            PerlinOffset = 0;
            PerlinSeed = 0;
            OutputOffset = 0;
            PreviousValue = 0;
            VolatileEmitter = volatileEmitter;

            OutputValue = 0;
            _inputSource = null;
        }

        public void Initialise(Actor actor, bool volatileEmitter = false)
        {
            _inputSource = new Source(actor);
            VolatileEmitter = volatileEmitter;
            if (VolatileEmitter) return;

            OutputOffset = Random.Range(StaticRange.x, StaticRange.y);
            PerlinOffset = Random.Range(0f, 1000f) * (1 + ParameterIndex);
            PerlinSeed = Mathf.PerlinNoise(PerlinOffset + ParameterIndex, PerlinOffset * 0.5f + ParameterIndex);
        }

        public ModulationComponent ProcessModulation()
        {
            OutputValue = _inputSource.GetInputValue();
            return BuildComponent(OutputValue);
        }

        public ModulationComponent BuildComponent(float outputValue)
        {
            return new ModulationComponent
            {
                StartValue = VolatileEmitter ? StaticRange.x : OutputOffset,
                EndValue = VolatileEmitter ? StaticRange.y : 0,
                ModValue = outputValue,
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
            if (VolatileEmitter ||
                !UsePerlin ||
                Mathf.Approximately(NoiseInfluence, 0f))
                return 0;

            PerlinOffset += NoiseSpeed * Time.deltaTime;
            return Mathf.PerlinNoise(PerlinSeed + PerlinOffset, (PerlinSeed + PerlinOffset) * 0.5f);
        }

        #endregion
    }
}