using System;
using UnityEngine;

namespace MaxVRAM.Audio
{
    [Serializable]
    public class WindowFunction
    {
        // Source: https://michaelkrzyzaniak.com/AudioSynthesis/2_Audio_Synthesis/11_Granular_Synthesis/1_Window_Functions/
        public enum FunctionSelect { sine = 0, hann = 1, hamming = 2, tukey = 3, gaussian = 4 }
        public FunctionSelect _WindowFunction = FunctionSelect.hann;

        public int _EnvelopeSize = 512;
        [Range(0.1f, 1.0f)] public float _TukeyHeight = 0.5f;
        [Range(0.1f, 1.0f)] public float _GaussianSigma = 0.5f;
        [Range(0.0f, 0.5f)] public float _LinearInOutFade = 0.1f;

        private float[] _WindowArray;
        public float[] WindowArray { get { return _WindowArray; } }

        public WindowFunction(FunctionSelect windowFunction, int envelopeSize, float tukeyHeight, float gaussianSigma, float linearInOutFade)
        {
            _WindowFunction = windowFunction;
            _EnvelopeSize = envelopeSize;
            _TukeyHeight = tukeyHeight;
            _GaussianSigma = gaussianSigma;
            _LinearInOutFade = linearInOutFade;
        }

        public float[] BuildWindowArray()
        {
            _WindowArray = new float[_EnvelopeSize];
            for (int i = 1; i < _WindowArray.Length; i++)
                _WindowArray[i] = AmplitudeAtIndex(i);
            return _WindowArray;
        }

        public float AmplitudeAtIndex(int index)
        {
            float amplitude = ApplyFunction(index);
            amplitude *= MaxMath.FadeInOut((float)index / (_EnvelopeSize - 1), _LinearInOutFade);
            return amplitude;
        }

        private float ApplyFunction(int index)
        {
            float amplitude = 1;

            switch (_WindowFunction)
            {
                case FunctionSelect.sine:
                    amplitude = Mathf.Sin(Mathf.PI * index / _EnvelopeSize);
                    break;
                case FunctionSelect.hann:
                    amplitude = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * index / _EnvelopeSize));
                    break;
                case FunctionSelect.hamming:
                    amplitude = 0.54f - 0.46f * Mathf.Cos(2 * Mathf.PI * index / _EnvelopeSize);
                    break;
                case FunctionSelect.tukey:
                    amplitude = 1 / (2 * _TukeyHeight) * (1 - Mathf.Cos(2 * Mathf.PI * index / _EnvelopeSize));
                    break;
                case FunctionSelect.gaussian:
                    amplitude = Mathf.Pow(Mathf.Exp(1), -0.5f * Mathf.Pow((index - (float)_EnvelopeSize / 2) / (_GaussianSigma * _EnvelopeSize / 2), 2));
                    break;
            }
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