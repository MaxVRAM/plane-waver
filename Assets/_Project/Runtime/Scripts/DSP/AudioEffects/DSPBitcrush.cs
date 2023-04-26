using Unity.Entities;
using UnityEngine;

namespace PlaneWaver.DSP
{
    /// <summary>
    /// Sample and hold style bit reduction
    /// </summary>
    public class DSPBitcrush : DSPClass
    {
        [Range(0f, 1f)] public float Mix = 1;
        [Range(0f, 50f)] public float CrushRatio;

        private int _sampleRate;

        public void Start()
        {
            _sampleRate = AudioSettings.outputSampleRate;
        }

        public override AudioEffectParameters GetDSPBufferElement()
        {
            var audioEffectParams = new AudioEffectParameters {
                AudioEffectType = AudioEffectTypes.Bitcrush,
                Mix = Mix,
                Value0 = CrushRatio
            };

            return audioEffectParams;
        }

        public static void ProcessDSP(
            AudioEffectParameters audioEffectParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer,
            DynamicBuffer<DSPSampleBufferElement> dspBuffer)
        {
            var count = 0;
            float previousSample = 0;

            for (var i = 0; i < sampleBuffer.Length; i++)
            {
                float outputSample;

                if (count >= audioEffectParams.Value0)
                {
                    outputSample = sampleBuffer[i].Value;
                    previousSample = outputSample;
                    count = 0;
                }
                else
                {
                    outputSample = previousSample;
                    count++;
                }

                sampleBuffer[i] = new GrainSampleBufferElement
                {
                    Value = Mathf.Lerp(sampleBuffer[i].Value, outputSample, audioEffectParams.Mix)
                };
            }
        }
    }
}