using Unity.Entities;
using UnityEngine;


namespace PlaneWaver.DSP
{
    /// <summary>
    /// Modulated delay mono chorus/flange/garble effect
    /// </summary>
    public class DSPFlange : DSPClass
    {
        [Range(0f, 1f)] public float Mix = 1;
        [Range(0f, 1f)] public float Original = 1;
        [Range(0.1f, 50f)] public float Delay = 15f;
        [Range(0.01f, 1f)] public float Depth = 0.1f;
        [Range(0.01f, 50f)] public float Frequency = 5f;
        [Range(0f, 0.99f)] public float Feedback = 0.3f;
        [Range(0f, 1f)] public float PhaseSync = 1f;
        private int _sampleRate;

        public void Start()
        {
            _sampleRate = AudioSettings.outputSampleRate;
        }

        public override AudioEffectParameters GetDSPBufferElement()
        {
            var audioEffectParams = new AudioEffectParameters {
                AudioEffectType = AudioEffectTypes.Flange,
                DelayBasedEffect = true,
                SampleRate = _sampleRate,
                SampleTail = (int)(Delay * Depth * _sampleRate / 1000) * 2,
                Mix = Mix,
                Value0 = Delay * _sampleRate / 1000,
                Value1 = Depth,
                Value2 = Frequency,
                Value3 = Feedback,
                Value4 = Original,
                Value5 = PhaseSync
            };

            return audioEffectParams;
        }

        public static void ProcessDSP(
            AudioEffectParameters audioEffectParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer,
            DynamicBuffer<DSPSampleBufferElement> dspBuffer)
        {
            //-- Set initial phase based on DSP time to sync effect between grains
            float phase = audioEffectParams.SampleStartTime *
                          (audioEffectParams.Value2 * 2 * Mathf.PI / audioEffectParams.SampleRate) *
                          audioEffectParams.Value5;
            while (phase >= Mathf.PI * 2)
                phase -= Mathf.PI * 2;


            for (var i = 0; i < sampleBuffer.Length; i++)
            {
                // Modulation (delay offset) is a -1 to 1 sine wave, multiplied by the depth (0 to 1), scaled to the current sample delay offset parameter
                float modIndex = (DSPUtilsDOTS.SineOcillator
                                          (ref phase, audioEffectParams.Value2, audioEffectParams.SampleRate) *
                                  audioEffectParams.Value1 *
                                  audioEffectParams.Value0);

                // Set delay index to current index, offset by the centre delay parameter and modulation value
                var writeIndex = (int)Mathf.Clamp(i + audioEffectParams.Value0 + modIndex, 0, sampleBuffer.Length - 1);
                //Debug.Log("Current Sample: " + i + "     Flange Mod: " + modIndex + "     Write Index: " + writeIndex);

                // Combine sample in delayed buffer with current pos original sample and current pos delay sample multipled by "feedback" value
                float delaySample = dspBuffer[writeIndex].Value +
                                    sampleBuffer[i].Value +
                                    dspBuffer[i].Value * audioEffectParams.Value3;

                // Write the delayed sample
                dspBuffer[writeIndex] = new DSPSampleBufferElement
                {
                    Value = delaySample
                };

                // Add the current input sample to the current DSP buffer for output
                dspBuffer[i] = new DSPSampleBufferElement
                {
                    Value = sampleBuffer[i].Value * audioEffectParams.Value4 + dspBuffer[i].Value
                };

                // Mix current sample with DSP buffer combowombo
                sampleBuffer[i] = new GrainSampleBufferElement
                {
                    Value = Mathf.Lerp(sampleBuffer[i].Value, dspBuffer[i].Value, audioEffectParams.Mix)
                };
            }
        }
    }
}