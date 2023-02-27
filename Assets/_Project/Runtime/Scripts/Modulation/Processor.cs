
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
            float input = inputValue * parameter.InputMultiplier;
            float inputNormalised = Mathf.InverseLerp(parameter.InputRange.x, parameter.InputRange.y, input);
            float preSmoothing = parameter.Accumulate ? parameter.PreviousValue + inputNormalised : inputNormalised;
            float smoothedValue = parameter.PreviousValue.Smooth(preSmoothing, parameter.Smoothing);
            parameter.PreviousValue = smoothedValue;

            if (parameter.VolatileEmitter)
                outputValue = parameter.LimiterMode switch
                {
                    ValueLimiter.Clip     => Mathf.Clamp(smoothedValue, 0, 1),
                    ValueLimiter.Repeat   => smoothedValue.RepeatNorm(),
                    ValueLimiter.PingPong => smoothedValue.PingPongNorm(),
                    _                     => smoothedValue
                };
            else
                outputValue = parameter.LimiterMode switch
                {
                    ValueLimiter.Clip => parameter.OutputOffset +
                                         Mathf.Pow(
                                             Mathf.Clamp01(smoothedValue),
                                             parameter.ModulationExponent
                                         ) * parameter.ModulationInfluence,
                    ValueLimiter.Repeat => smoothedValue.RepeatNorm(
                        parameter.ModulationInfluence,
                        parameter.OutputOffset
                    ),
                    ValueLimiter.PingPong => smoothedValue.PingPongNorm(
                        parameter.ModulationInfluence,
                        parameter.OutputOffset
                    ),
                    _ => smoothedValue
                };

            return outputValue;
        }
    }
}
