using System;
using UnityEngine;
using MaxVRAM.Extensions;
using PlaneWaver.Interaction;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public partial class Parameter
    {
        public PropertiesObject ParameterProperties;
        public ModulationInputObject ModulationInput;
        public ModulationDataObject ModulationData;

        private ProcessedValues _processedValues;
        private ActorObject _actor;
        public bool IsVolatileEmitter;
        private bool _isInitialised;
        private Vector2 _previousInitialRange;

        public Parameter(bool isVolatileEmitter = false)
        {
            IsVolatileEmitter = isVolatileEmitter;
            ModulationInput = new ModulationInputObject();
            _processedValues = new ProcessedValues(IsVolatileEmitter);
        }

        public void Reset()
        {
            ModulationInput = new ModulationInputObject();
            ModulationData = new ModulationDataObject(ParameterProperties, IsVolatileEmitter);
            ModulationData.Initialise();
            _processedValues = new ProcessedValues(IsVolatileEmitter);
        }

        public void Initialise(in ActorObject actor)
        {
            ModulationData.Initialise();
            _processedValues = new ProcessedValues(IsVolatileEmitter);
            _actor = actor;
            _isInitialised = true;
        }

        public ModulationComponent CreateModulationComponent()
        {
            if (!_isInitialised)
                throw new Exception("Parameter has not been initialised.");

            if (ModulationData.InitialRange != _previousInitialRange)
                ModulationData.InitialValue = ModulationData.GetNewInitialValue();
            _previousInitialRange = ModulationData.InitialRange;

            UpdateInputValue(ref _processedValues);
            ProcessModulation(ref _processedValues, ModulationData);
            ModulationComponent component = ModulationData.BuildComponent(_processedValues.Output);
            return component;
        }

        public void UpdateInputValue(ref ProcessedValues values, ActorObject actor = null)
        {
            if (actor == null && !_isInitialised)
                throw new Exception("Cannot get input value without actor from uninitialised parameter.");

            values.Instant = ModulationInput.IsInstant;
            values.Input = ModulationData.Enabled ? ModulationInput.GetValue(actor ? actor : _actor) : 0;
        }

        /// <summary>
        /// Performs parameter modulation processing on provided set of processed values object, using the
        /// parameter modulations from a given modulation data configuration object.
        /// </summary>
        /// <param name="modData">Modulation data object containing the processing configuration.</param>
        /// <param name="values">Processed values object for maintaining input, output, and mid-processing values.</param>
        public static void ProcessModulation(
            ref ProcessedValues values, in ModulationDataObject modData)
        {
            if (!modData.Enabled)
            {
                values.Output = modData.IsVolatileEmitter ? 0 : modData.InitialValue;
                return;
            }

            values.Normalised = values.Input.InverseLerp(modData.ModInputRange.x, modData.ModInputRange.y, modData.Absolute);
            // values.Normalised = Mathf.InverseLerp(modData.ModInputRange.x, modData.ModInputRange.y, values.Input);
            // values.Absolute = modData.Absolute ? Mathf.Abs(values.Normalised) : values.Normalised;
            values.Scaled = values.Normalised * modData.ModInputMultiplier;

            values.Accumulated = modData.Accumulate 
                    ? values.Accumulated + values.Scaled 
                    : values.Scaled;

            values.Raised = modData.LimiterMode != ModulationLimiter.Clip
                    ? values.Accumulated
                    : Mathf.Pow(Mathf.Clamp01(values.Accumulated), modData.InputExponent);

            values.Smoothed = values.Instant 
                    ? values.Raised
                    : values.Smoothed.Smooth(values.Raised, modData.Smoothing);

            float parameterRange = Mathf.Abs(modData.ParameterRange.y - modData.ParameterRange.x);
            
            if (modData.IsVolatileEmitter)
            {
                values.Limited = modData.LimiterMode switch {
                    ModulationLimiter.Clip     => Mathf.Clamp01(values.Smoothed),
                    ModulationLimiter.Wrap     => values.Smoothed.WrapNorm(),
                    ModulationLimiter.PingPong => values.Smoothed.PingPongNorm(),
                    _                          => Mathf.Clamp01(values.Smoothed)
                };
                float initialOffset = modData.ReversePath ? modData.InitialRange.y : modData.InitialRange.x;
                values.Output = values.Limited * modData.ModInfluence * parameterRange;
                values.Preview = Mathf.Clamp(values.Output + initialOffset, -parameterRange, parameterRange);
            }
            else
            {
                float initialOffset = Mathf.InverseLerp(modData.ParameterRange.x, modData.ParameterRange.y, modData.InitialValue);
                //float initialOffset = modData.InitialValue / parameterRange;
                values.Limited = modData.LimiterMode switch {
                    ModulationLimiter.Clip => Mathf.Clamp01(initialOffset + values.Smoothed * modData.ModInfluence),
                    ModulationLimiter.Wrap => values.Smoothed.WrapNorm(modData.ModInfluence, initialOffset),
                    ModulationLimiter.PingPong => values.Smoothed.PingPongNorm(modData.ModInfluence, initialOffset),
                    _ => Mathf.Clamp01(initialOffset + values.Smoothed * modData.ModInfluence)
                };
                values.Output = Mathf.Lerp(modData.ParameterRange.x, modData.ParameterRange.y, values.Limited);
                values.Preview = values.Output;
            }
        }

        public class ProcessedValues
        {
            public float Input;
            public float Normalised;
            public float Scaled;
            public float Accumulated;
            public float Smoothed;
            public float Raised;
            public float Limited;
            public float Output;
            public float Preview;
            public bool Instant;
            
            public ProcessedValues(bool instant = false)
            {
                Input = 0;
                Normalised = 0;
                Scaled = 0;
                Accumulated = 0;
                Smoothed = 0;
                Raised = 0;
                Limited = 0;
                Output = 0;
                Preview = 0;
                Instant = instant;
            }
        }
    }
}