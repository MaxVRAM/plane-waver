using MaxVRAM.Extensions;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    public struct ModulationValues
    {
        public float Initial;
        public float PerlinOffset;
        public readonly float PerlinSeed;
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
        public readonly Parameter ParameterRef;

        public ModulationValues(in Parameter parameter)
        {
            Input = 0;
            Initial = 0;
            Normalised = 0;
            Scaled = 0;
            Accumulated = 0;
            Smoothed = 0;
            Raised = 0;
            Limited = 0;
            Output = 0;
            Preview = 0;
            Instant = parameter.Input.Source.IsInstant;
            ParameterRef = parameter;
            PerlinOffset = 0;
            PerlinSeed = 0;

            if (parameter.IsVolatileEmitter) return;

            ResetInitialValue();
            Random.InitState((int)(Time.time * 1000));
            PerlinOffset = Random.Range(0f, 1000f) * (1 + parameter.ParameterIndex);
            PerlinSeed = Mathf.PerlinNoise(PerlinOffset + parameter.ParameterIndex, PerlinOffset * 0.5f + parameter.ParameterIndex);

        }

        public void ResetInitialValue()
        {
            Initial = Random.Range(ParameterRef.BaseRange.x, ParameterRef.BaseRange.y);
        }

        public float GetPerlinValue()
        {
            if (ParameterRef.IsVolatileEmitter ||
                !ParameterRef.Noise.Enabled ||
                !ParameterRef.Noise.UsePerlin ||
                Mathf.Approximately(ParameterRef.Noise.Amount, 0f))
                return 0;

            PerlinOffset += ParameterRef.Noise.PerlinSpeed * Time.deltaTime;
            return Mathf.PerlinNoise(PerlinSeed + PerlinOffset, (PerlinSeed + PerlinOffset) * 0.5f);
        }

        /// <summary>
        /// Processes the parameter modulation values for the given ModulationData object.
        /// </summary>
        public void Process()
        {
            if (!ParameterRef.Input.Enabled)
            {
                Output = ParameterRef.IsVolatileEmitter ? 0 : Initial;
                return;
            }

            Normalised = Input.InverseLerp(ParameterRef.Input.Range.x, ParameterRef.Input.Range.y, ParameterRef.Input.Absolute);
            Scaled = Normalised * ParameterRef.Input.Factor;
            Accumulated = ParameterRef.Input.Accumulate ? Accumulated + Scaled : Scaled;

            Raised = ParameterRef.Output.Limiter != ModulationLimiter.Clip
                    ? Accumulated
                    : Mathf.Pow(Mathf.Clamp01(Accumulated), ParameterRef.Input.Exponent);

            Smoothed = Instant ? Raised : Smoothed.Smooth(Raised, ParameterRef.Input.Smoothing);

            float parameterRange = Mathf.Abs(ParameterRef.Range.y - ParameterRef.Range.x);

            if (ParameterRef.IsVolatileEmitter)
            {
                Limited = ParameterRef.Output.Limiter switch {
                    ModulationLimiter.Clip     => Mathf.Clamp01(Smoothed),
                    ModulationLimiter.Wrap     => Smoothed.WrapNorm(),
                    ModulationLimiter.PingPong => Smoothed.PingPongNorm(),
                    _                          => Mathf.Clamp01(Smoothed)
                };

                float initialOffset = ParameterRef.ReversePath ? ParameterRef.BaseRange.y : ParameterRef.BaseRange.x;
                Output = Limited * ParameterRef.Output.Amount * parameterRange;
                Preview = Mathf.Clamp(Output + initialOffset, -parameterRange, parameterRange);
            }
            else
            {
                float initialOffset = Mathf.InverseLerp(ParameterRef.Range.x, ParameterRef.Range.y, Initial);

                Limited = ParameterRef.Output.Limiter switch {
                    ModulationLimiter.Clip     => Mathf.Clamp01(initialOffset + Smoothed * ParameterRef.Output.Amount),
                    ModulationLimiter.Wrap     => Smoothed.WrapNorm(ParameterRef.Output.Amount, initialOffset),
                    ModulationLimiter.PingPong => Smoothed.PingPongNorm(ParameterRef.Output.Amount, initialOffset),
                    _                          => Mathf.Clamp01(initialOffset + Smoothed * ParameterRef.Output.Amount)
                };
                Output = Mathf.Lerp(ParameterRef.Range.x, ParameterRef.Range.y, Limited);
                Preview = Output;
            }
        }
    }
}
