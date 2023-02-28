using Unity.Entities;
using UnityEngine;

// Sample and hold style bit reduction
public class DSP_Bitcrush : DSP_Class
{
    [Range(0f,1f)]
    [SerializeField]
    public float _Mix = 1;
    [Range(0f, 50f)]
    [SerializeField]
    public float _CrushRatio;

    int _SampleRate;

    public void Start()
    {
        _SampleRate = AudioSettings.outputSampleRate;
    }

    public override AudioEffectParameters GetDSPBufferElement()
    {
        AudioEffectParameters audioEffectParams = new AudioEffectParameters();
        audioEffectParams.AudioEffectType = AudioEffectTypes.Bitcrush;
        audioEffectParams.Mix = _Mix;
        audioEffectParams.Value0 = _CrushRatio;

        return audioEffectParams;
    }

    public static void ProcessDSP(AudioEffectParameters audioEffectParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer)
    {
        int count = 0;
        float previousSample = 0;
        float outputSample = 0;

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
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

            sampleBuffer[i] = new GrainSampleBufferElement { Value = Mathf.Lerp(sampleBuffer[i].Value, outputSample, audioEffectParams.Mix) };
        }
    }
}