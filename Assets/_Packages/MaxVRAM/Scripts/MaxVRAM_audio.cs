using System;
using UnityEngine;

// PROJECT AUDIO CONFIGURATION NOTES
// ---------------------------------
// DSP Buffer size in audio settings
// Best performance - 46.43991
// Good latency - 23.21995
// Best latency - 11.60998

namespace MaxVRAM.Audio
{
    [Serializable]
    public class Windowing
    {
        // Source: https://michaelkrzyzaniak.com/AudioSynthesis/2_Audio_Synthesis/11_Granular_Synthesis/1_Window_Functions/
        public enum FunctionSelect { Sine = 0, Hann = 1, Hamming = 2, Tukey = 3, Gaussian = 4 }
        public FunctionSelect WindowFunction;

        public int EnvelopeSize;
        [Range(0.1f, 1.0f)] public float TukeyHeight;
        [Range(0.1f, 1.0f)] public float GaussianSigma;
        [Range(0.0f, 0.5f)] public float LinearInOutFade;

        public float[] WindowArray { get; private set; }

        public Windowing(FunctionSelect windowFunction, int envelopeSize = 512, float tukeyHeight = 0.5f, float gaussianSigma = 0.5f, float linearInOutFade = 0.01f)
        {
            WindowFunction = windowFunction;
            EnvelopeSize = envelopeSize;
            TukeyHeight = tukeyHeight;
            GaussianSigma = gaussianSigma;
            LinearInOutFade = linearInOutFade;
        }
        
        public float[] BuildWindowArray()
        {
            WindowArray = new float[EnvelopeSize];
            for (var i = 1; i < WindowArray.Length; i++)
                WindowArray[i] = AmplitudeAtIndex(i);
            return WindowArray;
        }

        public float AmplitudeAtIndex(int index)
        {
            float amplitude = ApplyFunction(index);
            amplitude *= MaxMath.FadeInOut((float)index / (EnvelopeSize - 1), LinearInOutFade);
            return amplitude;
        }

        private float ApplyFunction(int index)
        {
            float amplitude = WindowFunction switch
            {
                FunctionSelect.Sine    => Mathf.Sin(Mathf.PI * index / EnvelopeSize),
                FunctionSelect.Hann    => 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * index / EnvelopeSize)),
                FunctionSelect.Hamming => 0.54f - 0.46f * Mathf.Cos(2 * Mathf.PI * index / EnvelopeSize),
                FunctionSelect.Tukey =>
                        1 / (2 * TukeyHeight) * (1 - Mathf.Cos(2 * Mathf.PI * index / EnvelopeSize)),
                FunctionSelect.Gaussian => Mathf.Pow(
                    Mathf.Exp(1),
                    -0.5f * Mathf.Pow((index - (float)EnvelopeSize / 2) / (GaussianSigma * EnvelopeSize / 2), 2)
                ),
                _ => 1
            };
            return Mathf.Clamp01(amplitude);
        }
    }

    public struct ScaleAmplitude
    {
        public static float SpeakerOffsetFactor(Vector3 target, Vector3 listener, Vector3 speaker)
        {
            // TODO: Check if this is proportionally correct
            float speakerDist = Mathf.Abs((listener - speaker).magnitude);
            float targetDist = Mathf.Abs((listener - target).magnitude);
            return speakerDist / targetDist;
        }

        public static float ListenerDistanceVolume(Vector3 source, Vector3 target, float maxDistance)
        {
            // Inverse square attenuation for audio sources based on distance from the listener
            float sourceDistance = Mathf.Clamp(Mathf.Abs((source - target).magnitude) / maxDistance, 0f, 1f);
            return Mathf.Clamp(Mathf.Pow(500, -0.5f * sourceDistance), 0f, 1f);
        }

        public static float ListenerDistanceVolume(float distance, float maxDistance)
        {
            float normalisedDistance = Mathf.Clamp(distance / maxDistance, 0f, 1f);
            return Mathf.Clamp(Mathf.Pow(500, -0.5f * normalisedDistance), 0f, 1f);
        }
        public static float ListenerDistanceVolume(float normalisedDistance)
        {
            normalisedDistance = Mathf.Clamp(normalisedDistance, 0f, 1f);
            return Mathf.Clamp(Mathf.Pow(500, -0.5f * normalisedDistance), 0f, 1f);
        }
    }

    public struct ScaleFrequency
    {
        public static float FreqToMel(float freq)
        {
            // ref: https://en.wikipedia.org/wiki/Mel_scale
            float mel = 2595 * Mathf.Log10(1 + freq / 700);
            return mel;
        }

        public static float MelToFreq(float mel)
        {
            // ref: https://en.wikipedia.org/wiki/Mel_scale
            float freq = 700 * (Mathf.Pow(10, mel / 2595) - 1);
            return freq;
        }

        public static float FreqToNorm(float freq)
        {
            float norm = 2595 * Mathf.Log10(1 + freq / 700) / 3800;
            return Mathf.Clamp(norm, 0, 1);
        }

        public static float NormToFreq(float norm)
        {
            float freq = 700 * (Mathf.Pow(10, (norm * 3800) / 2595) - 1);
            return Mathf.Clamp(freq, 20, 20000);
        }
    }
}