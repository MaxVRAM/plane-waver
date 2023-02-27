
using UnityEngine;
using MaxVRAM.Extensions;

namespace PlaneWaver
{ 
    public static class ModulationProcessor
    {
        // TODO - Turn this into a DOTS job
        
        public static float ProcessModulation(
                ModulationParameter parameter, float inputValue, 
                ref float previousSmoothedValue, float outputOffset = 0f)
        {
            float outputValue = 0;
            float input = inputValue * parameter.InputMultiplier;
            float inputNormalised = Mathf.InverseLerp(parameter.InputRange.x, parameter.InputRange.y, input);
            float preSmoothing = parameter.Accumulate ? previousSmoothedValue + inputNormalised : inputNormalised;
            float smoothedValue = previousSmoothedValue.Smooth(preSmoothing, parameter.Smoothing);
            previousSmoothedValue = smoothedValue;
            
            if (parameter.ForCollisionEmitter)
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
                        ValueLimiter.Clip     => outputOffset + Mathf.Pow(Mathf.Clamp01(smoothedValue), parameter.ModulationExponent) * parameter.ModulationAmount,
                        ValueLimiter.Repeat   => smoothedValue.RepeatNorm(parameter.ModulationAmount, outputOffset),
                        ValueLimiter.PingPong => smoothedValue.PingPongNorm(parameter.ModulationAmount, outputOffset),
                        _                     => smoothedValue
                };

            return outputValue;
        }
    }
}
