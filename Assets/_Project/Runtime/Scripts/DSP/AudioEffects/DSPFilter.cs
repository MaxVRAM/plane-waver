using Unity.Entities;
using UnityEngine;
using MaxVRAM.Audio;


namespace PlaneWaver.DSP
{
    /// <summary>
    /// A classic bi-quad filter design
    /// </summary>
    public class DSPFilter : DSPClass
    {
        public enum FilterTypes { LowPass, HiPass, BandPass, PeakNotch, None }

        [Range(0f, 1f)] public float Mix = 1;
        public FilterTypes FilterType;
        [Range(0f, 1f)] public float FilterCutoffNorm = 0.5f;
        [Range(0.05f, 1)] public float FilterGain = 1;
        [Range(0.1f, 5)] public float FilterQ = 1;
        private int _sampleRate;

        public void Start()
        {
            _sampleRate = AudioSettings.outputSampleRate;
        }

        private class FilterCoefficientClass
        {
            public float A0;
            public float A1;
            public float A2;
            public float B1;
            public float B2;
        }

        public override AudioEffectParameters GetDSPBufferElement()
        {
            float cutoffFreq = ScaleFrequency.NormToFreq(Mathf.Clamp(FilterCutoffNorm, 0f, 1f));
            float gain = Mathf.Clamp(FilterGain, 0.5f, 1f);
            float q = Mathf.Clamp(FilterQ, 0.1f, 5f);

            //--  Construct bi-quad filter coefficients
            FilterCoefficientClass newCoefficients = FilterType switch {
                FilterTypes.LowPass   => LowPass(cutoffFreq, gain, q, _sampleRate),
                FilterTypes.HiPass    => HiPass(cutoffFreq, gain, q, _sampleRate),
                FilterTypes.BandPass  => BandPass(cutoffFreq, gain, q, _sampleRate),
                FilterTypes.PeakNotch => PeakNotch(cutoffFreq, gain, q, _sampleRate),
                _                     => AllPass(cutoffFreq, gain, q, _sampleRate)
            };

            var audioEffectParams = new AudioEffectParameters {
                AudioEffectType = AudioEffectTypes.Filter,
                SampleRate = _sampleRate,
                Mix = Mix,
                Value0 = newCoefficients.A0,
                Value1 = newCoefficients.A1,
                Value2 = newCoefficients.A2,
                Value3 = newCoefficients.B1,
                Value4 = newCoefficients.B2
            };

            return audioEffectParams;
        }

        public static void ProcessDSP(
            AudioEffectParameters audioEffectParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer,
            DynamicBuffer<DSPSampleBufferElement> dspBuffer)
        {
            float previousX1 = 0;
            float previousX2 = 0;
            float previousY1 = 0;
            float previousY2 = 0;

            for (var i = 0; i < sampleBuffer.Length; i++)
            {
                // Apply coefficients to input signal and history data
                float outputSample = (sampleBuffer[i].Value * audioEffectParams.Value0 +
                                      previousX1 * audioEffectParams.Value1 +
                                      previousX2 * audioEffectParams.Value2) -
                                     (previousY1 * audioEffectParams.Value3 + previousY2 * audioEffectParams.Value4);

                // Set history states for signal data
                previousX2 = previousX1;
                previousX1 = sampleBuffer[i].Value;
                previousY2 = previousY1;
                previousY1 = outputSample;

                sampleBuffer[i] = new GrainSampleBufferElement
                {
                    Value = Mathf.Lerp(sampleBuffer[i].Value, outputSample, audioEffectParams.Mix)
                };
            }
        }

        private static FilterCoefficientClass LowPass(float cutoff, float gain, float q, int sampleRate)
        {

            float omega = cutoff * 2 * Mathf.PI / sampleRate;
            float sn = Mathf.Sin(omega);
            float cs = Mathf.Cos(omega);
            float igain = 1.0f / gain;
            float oneOverQ = 1.0f / q;
            float alpha = sn * 0.5f * oneOverQ;
            float b0 = 1.0f / (1.0f + alpha);
            float a2 = (1.0f - cs) * 0.5f * b0;
            
            var newFilterCoefficients = new FilterCoefficientClass {
                A2 = a2,
                A0 = a2,
                A1 = (1.0f - cs) * b0,
                B1 = (-2.0f * cs) * b0,
                B2 = (1.0f - alpha) * b0
            };

            return newFilterCoefficients;
        }

        private static FilterCoefficientClass HiPass(float cutoff, float gain, float q, float sampleRate)
        {

            float omega = cutoff * 2 * Mathf.PI / sampleRate;
            float sn = Mathf.Sin(omega);
            float cs = Mathf.Cos(omega);
            float alpha = sn * 0.5f / q;
            float b0 = 1.0f / (1.0f + alpha);
            float a2 = (1.0f - cs) * 0.5f * b0;
            
            var newFilterCoefficients = new FilterCoefficientClass {
                A2 = a2,
                A0 = a2,
                A1 = -(1.0f + cs) * b0,
                B1 = (-2.0f * cs) * b0,
                B2 = (1.0f - alpha) * b0
            };

            return newFilterCoefficients;
        }

        private static FilterCoefficientClass BandPass(float cutoff, float gain, float q, float sampleRate)
        {

            float omega = cutoff * 2 * Mathf.PI / sampleRate;
            float sn = Mathf.Sin(omega);
            float cs = Mathf.Cos(omega);
            float alpha = sn * 0.5f / q;
            float b0 = 1.0f / (1.0f + alpha);

            var newFilterCoefficients = new FilterCoefficientClass {
                A0 = alpha * b0,
                A1 = 0.0f,
                A2 = -alpha * b0,
                B1 = -2.0f * cs * b0,
                B2 = (1.0f - alpha) * b0
            };

            return newFilterCoefficients;
        }

        private static FilterCoefficientClass PeakNotch(float cutoff, float gain, float q, float sampleRate)
        {

            float omega = cutoff * 2 * Mathf.PI / sampleRate;
            float sn = Mathf.Sin(omega);
            float cs = Mathf.Cos(omega);
            float alpha = sn * 0.5f / q;
            float a = Mathf.Sqrt(gain);
            float oneOverA = 1.0f / a;
            float b0 = 1.0f / (1.0f + alpha * oneOverA);
            float b1 = -2.0f * cs * b0;

            var newFilterCoefficients = new FilterCoefficientClass {
                A0 = (1.0f + alpha * a) * b0,
                B1 = b1,
                A1 = b1,
                A2 = (1.0f - alpha * a) * b0,
                B2 = (1.0f - alpha * oneOverA) * b0
            };

            return newFilterCoefficients;
        }

        private static FilterCoefficientClass AllPass(float cutoff, float gain, float q, float sampleRate)
        {
            float omega = cutoff * 2 * Mathf.PI / sampleRate;
            float sn = Mathf.Sin(omega);
            float cs = Mathf.Cos(omega);
            float alpha = sn * 0.5f / q;
            float b0 = 1.0f / (1.0f + alpha);
            float b1 = -2.0f * cs * b0;
            float a0 = (1.0f - alpha) * b0;
            
            var newFilterCoefficients = new FilterCoefficientClass {
                B1 = b1,
                A1 = b1,
                A0 = a0,
                B2 = a0,
                A2 = 1.0f
            };

            return newFilterCoefficients;
        }
    }
}