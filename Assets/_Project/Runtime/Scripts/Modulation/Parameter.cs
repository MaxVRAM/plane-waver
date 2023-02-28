
using UnityEngine;
using System;

using PlaneWaver.Interaction;

using Unity.Entities;
using Random = UnityEngine.Random;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public struct Parameter
    {
        #region DEFINITIONS
        
        public readonly int ParameterIndex;
        public readonly Vector2 ParameterRange;
        public Vector2 StaticRange;
        public SourceSelection SourceSelection;
        // public ModulationSourceGroups SourceGroup;
        // public ModulationSourceMisc SourceMisc;
        // public ModulationSourceActor SourceActor;
        // public ModulationSourceRelational SourceRelational;
        // public ModulationSourceCollision SourceCollision;
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
        
        #endregion
        
        #region CONSTRUCTOR / BUILDERS

        public Parameter(ParamDefault defaultValues, bool volatileEmitter = false)
        {
            ParameterIndex = defaultValues.Index;
            ParameterRange = defaultValues.Range;
            StaticRange = new Vector2(0f,1f);
            SourceSelection = new SourceSelection();
            // SourceGroup = ModulationSourceGroups.General;
            // SourceMisc = ModulationSourceMisc.Disabled;
            // SourceActor = ModulationSourceActor.Scale;
            // SourceRelational = ModulationSourceRelational.DistanceX;
            // SourceCollision = ModulationSourceCollision.CollisionForce;
            ModInputRange = new Vector2(0f,1f);
            ModInputMultiplier = 1;
            Accumulate = false;
            Smoothing = 0.2f;
            ModExponent = 1;
            ModInfluence = 0;
            FixedStart = defaultValues.FixedStart;
            FixedEnd =  defaultValues.FixedEnd;
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
        }

        public void Initialise(bool volatileEmitter = false)
        {
            VolatileEmitter = volatileEmitter;
            if (VolatileEmitter) return;
            
            OutputOffset = Random.Range(StaticRange.x, StaticRange.y);
            PerlinOffset = Random.Range(0f, 1000f) * (1 + ParameterIndex);
            PerlinSeed = Mathf.PerlinNoise(PerlinOffset + ParameterIndex, PerlinOffset * 0.5f + ParameterIndex);
        }
        
        public float GetModulationValue(in Actor actor)
        {
            if (Mathf.Approximately(ModInfluence, 0f))
                return 0;

            return 0;
        }
        
        public ModComponent BuildComponent(float modulationValue)
        {
            return new ModComponent
            {
                StartValue = VolatileEmitter ? StaticRange.x : OutputOffset,
                EndValue = VolatileEmitter ? StaticRange.y : 0,
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