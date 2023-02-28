
using UnityEngine;
using MaxVRAM.Extensions;

namespace PlaneWaver.Modulation
{ 
    public static class Processor
    {
        // TODO - Turn this into a DOTS job
        
        /// <summary>
        /// Generates an emitter modulation value based on the source input value and the ModulationParameter.
        /// </summary>
        /// <param name="parameter">A struct of parameters that define how the input value is processed.</param>
        /// <param name="inputValue">Can be any float value. Generally supplied via a modulation input source.</param>
        /// <returns></returns>
        public static float ProcessModulation(Parameter parameter, float inputValue)
        {
            float outputValue = 0;
            float input = inputValue * parameter.ModInputMultiplier;
            float inputNormalised = Mathf.InverseLerp(parameter.ModInputRange.x, parameter.ModInputRange.y, input);
            float preSmoothing = parameter.Accumulate ? parameter.PreviousValue + inputNormalised : inputNormalised;
            float smoothedValue = parameter.PreviousValue.Smooth(preSmoothing, parameter.Smoothing);
            parameter.PreviousValue = smoothedValue;

            if (parameter.VolatileEmitter)
                outputValue = parameter.LimiterMode switch
                {
                    ModulationLimiter.Clip     => Mathf.Clamp(smoothedValue, 0, 1),
                    ModulationLimiter.Repeat   => smoothedValue.RepeatNorm(),
                    ModulationLimiter.PingPong => smoothedValue.PingPongNorm(),
                    _                     => smoothedValue
                };
            else
                outputValue = parameter.LimiterMode switch
                {
                    ModulationLimiter.Clip => parameter.OutputOffset +
                                         Mathf.Pow(
                                             Mathf.Clamp01(smoothedValue),
                                             parameter.ModExponent
                                         ) * parameter.ModInfluence,
                    ModulationLimiter.Repeat => smoothedValue.RepeatNorm(
                        parameter.ModInfluence,
                        parameter.OutputOffset
                    ),
                    ModulationLimiter.PingPong => smoothedValue.PingPongNorm(
                        parameter.ModInfluence,
                        parameter.OutputOffset
                    ),
                    _ => smoothedValue
                };

            return outputValue;
        }
    }
}
