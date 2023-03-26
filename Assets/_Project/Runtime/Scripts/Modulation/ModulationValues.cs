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
        public readonly ModulationData Data;

        public ModulationValues(in ModulationData data, bool instant = false)
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
            Instant = instant;
            Data = data;
            PerlinOffset = 0;
            PerlinSeed = 0;

            if (Data.IsVolatileEmitter) return;

            ResetInitialValue();
            Random.InitState((int)(Time.time * 1000));
            PerlinOffset = Random.Range(0f, 1000f) * (1 + Data.ParameterIndex);
            PerlinSeed = Mathf.PerlinNoise(PerlinOffset + Data.ParameterIndex, PerlinOffset * 0.5f + Data.ParameterIndex);

        }

        public void ResetInitialValue()
        {
            Initial = Random.Range(Data.InitialRange.x, Data.InitialRange.y);
        }

        public float GetPerlinValue()
        {
            if (Data.IsVolatileEmitter ||
                !Data.NoiseEnabled ||
                !Data.UsePerlin ||
                Mathf.Approximately(Data.NoiseInfluence, 0f))
                return 0;

            PerlinOffset += Data.PerlinSpeed * Time.deltaTime;
            return Mathf.PerlinNoise(PerlinSeed + PerlinOffset, (PerlinSeed + PerlinOffset) * 0.5f);
        }

        /// <summary>
        /// Processes the parameter modulation values for the given ModulationData object.
        /// </summary>
        public void Process()
        {
            if (!Data.Enabled)
            {
                Output = Data.IsVolatileEmitter ? 0 : Initial;
                return;
            }

            Normalised = Input.InverseLerp(Data.ModInputRange.x, Data.ModInputRange.y, Data.Absolute);
            Scaled = Normalised * Data.ModInputMultiplier;
            Accumulated = Data.Accumulate ? Accumulated + Scaled : Scaled;

            Raised = Data.LimiterMode != ModulationLimiter.Clip
                    ? Accumulated
                    : Mathf.Pow(Mathf.Clamp01(Accumulated), Data.InputExponent);

            Smoothed = Instant ? Raised : Smoothed.Smooth(Raised, Data.Smoothing);

            float parameterRange = Mathf.Abs(Data.ParameterRange.y - Data.ParameterRange.x);

            if (Data.IsVolatileEmitter)
            {
                Limited = Data.LimiterMode switch {
                    ModulationLimiter.Clip     => Mathf.Clamp01(Smoothed),
                    ModulationLimiter.Wrap     => Smoothed.WrapNorm(),
                    ModulationLimiter.PingPong => Smoothed.PingPongNorm(),
                    _                          => Mathf.Clamp01(Smoothed)
                };

                float initialOffset = Data.ReversePath ? Data.InitialRange.y : Data.InitialRange.x;
                Output = Limited * Data.ModInfluence * parameterRange;
                Preview = Mathf.Clamp(Output + initialOffset, -parameterRange, parameterRange);
            }
            else
            {
                float initialOffset = Mathf.InverseLerp(Data.ParameterRange.x, Data.ParameterRange.y, Initial);

                Limited = Data.LimiterMode switch {
                    ModulationLimiter.Clip     => Mathf.Clamp01(initialOffset + Smoothed * Data.ModInfluence),
                    ModulationLimiter.Wrap     => Smoothed.WrapNorm(Data.ModInfluence, initialOffset),
                    ModulationLimiter.PingPong => Smoothed.PingPongNorm(Data.ModInfluence, initialOffset),
                    _                          => Mathf.Clamp01(initialOffset + Smoothed * Data.ModInfluence)
                };
                Output = Mathf.Lerp(Data.ParameterRange.x, Data.ParameterRange.y, Limited);
                Preview = Output;
            }
        }
    }
}
