using MaxVRAM.Extensions;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    public partial class Modulation
    {
        // TODO - Turn this into a DOTS job

        /// <summary>
        ///     Generates an emitter modulation value based on the source input value and the ModulationParameter.
        /// </summary>
        /// <param name="data">Parameter data struct defining the input value processing policy.</param>
        /// <param name="inputValue">Can be any float value. Generally supplied via a modulation input source.</param>
        /// <param name="previousSmoothed">Referenced runtime-persistent float for smoothing calculations.</param>
        /// <returns></returns>
        public static float ProcessModulation(in Data data, float inputValue, ref float previousSmoothed)
        {
            float outputValue = 0;
            float input = inputValue * data.ModInputMultiplier;
            float inputNormalised = Mathf.InverseLerp(data.ModInputRange.x, data.ModInputRange.y, input);
            float preSmoothing = data.Accumulate ? previousSmoothed + inputNormalised : inputNormalised;
            float smoothedValue = data.PreviousValue.Smooth(preSmoothing, data.Smoothing);
            previousSmoothed = smoothedValue;

            if (data.VolatileEmitter)
                outputValue = data.LimiterMode switch
                {
                    ModulationLimiter.Clip     => Mathf.Clamp(smoothedValue, 0, 1),
                    ModulationLimiter.Repeat   => smoothedValue.RepeatNorm(),
                    ModulationLimiter.PingPong => smoothedValue.PingPongNorm(),
                    _                          => smoothedValue
                };
            else
                outputValue = data.LimiterMode switch
                {
                    ModulationLimiter.Clip => data.OutputOffset +
                                              Mathf.Pow(Mathf.Clamp01(smoothedValue), data.ModExponent) *
                                              data.ModInfluence,
                    ModulationLimiter.Repeat   => smoothedValue.RepeatNorm(data.ModInfluence, data.OutputOffset),
                    ModulationLimiter.PingPong => smoothedValue.PingPongNorm(data.ModInfluence, data.OutputOffset),
                    _                          => smoothedValue
                };

            return outputValue;
        }
    }
}