using Unity.Entities;
using UnityEngine;
using MaxVRAM.Audio;

// A classic bi-quad filter design
public class DSP_Filter : DSP_Class
{
    public enum FilterType { LowPass, HiPass, BandPass, PeakNotch, None }

    [Range(0f, 1f)]
    [SerializeField]
    public float _Mix = 1;

    [SerializeField]
    public FilterType _FilterType;

    [Range(0f, 1f)]
    [SerializeField]
    float _FilterCutoffNorm = 0.5f;

    [Range(0.05f, 1)]
    [SerializeField]
    float _FilterGain = 1;

    [Range(0.1f, 5)]
    [SerializeField]
    float _FilterQ = 1;

    int _SampleRate;

    public void Start()
    {
        _SampleRate = AudioSettings.outputSampleRate;
    }

    private class FilterCoefficientClass
    {
        public float a0;
        public float a1;
        public float a2;
        public float b1;
        public float b2;
    }

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspParams = new DSPParametersElement();

        float cutoffFreq = ScaleFrequency.NormToFreq(Mathf.Clamp(_FilterCutoffNorm, 0f, 1f));
        float gain = Mathf.Clamp(_FilterGain, 0.5f, 1f);
        float q = Mathf.Clamp(_FilterQ, 0.1f, 5f);

        //--  Construct bi-quad filter coefficients
        FilterCoefficientClass newCoefficients;

        if (_FilterType == FilterType.LowPass)
            newCoefficients = LowPass(cutoffFreq, gain, q, _SampleRate);
        else if (_FilterType == FilterType.HiPass)
            newCoefficients = HiPass(cutoffFreq, gain, q, _SampleRate);
        else if (_FilterType == FilterType.BandPass)
            newCoefficients = BandPass(cutoffFreq, gain, q, _SampleRate);
        else if (_FilterType == FilterType.PeakNotch)
            newCoefficients = PeakNotch(cutoffFreq, gain, q, _SampleRate);
        else
            newCoefficients = AllPass(cutoffFreq, gain, q, _SampleRate);

        dspParams._DSPType = DSPTypes.Filter;
        dspParams._SampleRate = _SampleRate;
        dspParams._Mix = _Mix;
        dspParams._Value0 = newCoefficients.a0;
        dspParams._Value1 = newCoefficients.a1;
        dspParams._Value2 = newCoefficients.a2;
        dspParams._Value3 = newCoefficients.b1;
        dspParams._Value4 = newCoefficients.b2;

        return dspParams;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer)
    {
        float outputSample;

        float previousX1 = 0;
        float previousX2 = 0;
        float previousY1 = 0;
        float previousY2 = 0;

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            // Apply coefficients to input signal and history data
            outputSample = (sampleBuffer[i].Value * dspParams._Value0 +
                             previousX1 * dspParams._Value1 +
                             previousX2 * dspParams._Value2) -
                             (previousY1 * dspParams._Value3 +
                             previousY2 * dspParams._Value4);

            // Set history states for signal data
            previousX2 = previousX1;
            previousX1 = sampleBuffer[i].Value;
            previousY2 = previousY1;
            previousY1 = outputSample;

            sampleBuffer[i] = new GrainSampleBufferElement { Value = Mathf.Lerp(sampleBuffer[i].Value, outputSample, dspParams._Mix) };
        }
    }

    private static FilterCoefficientClass LowPass(float cutoff, float gain, float q, int sampleRate)
    {
        FilterCoefficientClass newFilterCoefficients = new FilterCoefficientClass();

        float omega = cutoff * 2 * Mathf.PI / sampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float igain = 1.0f / gain;
        float one_over_Q = 1.0f / q;
        float alpha = sn * 0.5f * one_over_Q;

        float b0 = 1.0f / (1.0f + alpha);

        newFilterCoefficients.a2 = ((1.0f - cs) * 0.5f) * b0;
        newFilterCoefficients.a0 = newFilterCoefficients.a2;
        newFilterCoefficients.a1 = (1.0f - cs) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficientClass HiPass(float cutoff, float gain, float q, float sampleRate)
    {
        FilterCoefficientClass newFilterCoefficients = new FilterCoefficientClass();

        float omega = cutoff * 2 * Mathf.PI / sampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / q;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.a2 = ((1.0f + cs) * 0.5f) * b0;
        newFilterCoefficients.a0 = newFilterCoefficients.a2;
        newFilterCoefficients.a1 = -(1.0f + cs) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficientClass BandPass(float cutoff, float gain, float q, float sampleRate)
    {
        FilterCoefficientClass newFilterCoefficients = new FilterCoefficientClass();

        float omega = cutoff * 2 * Mathf.PI / sampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / q;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.a0 = alpha * b0;
        newFilterCoefficients.a1 = 0.0f;
        newFilterCoefficients.a2 = -alpha * b0;
        newFilterCoefficients.b1 = -2.0f * cs * b0;
        newFilterCoefficients.b2 = (1.0f - alpha) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficientClass PeakNotch(float cutoff, float gain, float q, float sampleRate)
    {
        FilterCoefficientClass newFilterCoefficients = new FilterCoefficientClass();

        float omega = cutoff * 2 * Mathf.PI / sampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / q;

        float A = Mathf.Sqrt(gain);
        float one_over_A = 1.0f / A;
        float b0 = 1.0f / (1.0f + alpha * one_over_A);

        newFilterCoefficients.a0 = (1.0f + alpha * A) * b0;
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.a1 = newFilterCoefficients.b1;
        newFilterCoefficients.a2 = (1.0f - alpha * A) * b0;
        newFilterCoefficients.b2 = (1.0f - alpha * one_over_A) * b0;

        return newFilterCoefficients;
    }

    private static FilterCoefficientClass AllPass(float cutoff, float gain, float q, float sampleRate)
    {
        FilterCoefficientClass newFilterCoefficients = new FilterCoefficientClass();

        float omega = cutoff * 2 * Mathf.PI / sampleRate;
        float sn = Mathf.Sin(omega);
        float cs = Mathf.Cos(omega);

        float alpha = sn * 0.5f / q;

        float b0 = 1.0f / (1.0f + alpha);
        newFilterCoefficients.b1 = (-2.0f * cs) * b0;
        newFilterCoefficients.a1 = newFilterCoefficients.b1;
        newFilterCoefficients.a0 = (1.0f - alpha) * b0;
        newFilterCoefficients.b2 = newFilterCoefficients.a0;
        newFilterCoefficients.a2 = 1.0f;

        return newFilterCoefficients;
    }
}
