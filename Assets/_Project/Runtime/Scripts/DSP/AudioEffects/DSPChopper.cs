using Unity.Entities;
using UnityEngine;


namespace PlaneWaver.DSP
{
    /// <summary>
    /// A wonderfully glitchy DSP effect, ported from Graham Wakefield's 2012 example in Max MSP's Gen DSP effects
    /// </summary>
    public class DSPChopper : DSPClass
    {
        private const int NumSegments = 32;

        [Range(0f, 1f)] public float Mix = 1;
        [Range(1f, 16f)] public float Crossings = 2;
        [Range(32, 2048)] public int MaxSegmentLength = 2048;
        [Range(32, 512)] public int MinSegmentLength = 128;
        [Range(1f, 16f)] public float Repeats = 1;
        public PlayModes PlayMode;
        public PitchedModes PitchedMode;
        [Range(1f, 2000f)] public float Frequency = 220;
        [Range(0f, 16f)] public float Rate = 1;
        
        public enum PlayModes { Forward, Reverse, Walk, Random }
        public enum PitchedModes { Normal, Ascending, Descending, Pitched }
        private int _sampleRate;

        public void Start()
        {
            _sampleRate = AudioSettings.outputSampleRate;
        }

        public override AudioEffectParameters GetDSPBufferElement()
        {
            var audioEffectParams = new AudioEffectParameters {
                AudioEffectType = AudioEffectTypes.Chopper,
                DelayBasedEffect = true,
                SampleRate = _sampleRate,
                SampleTail = NumSegments * 2,
                Mix = Mix,
                Value0 = Crossings,
                Value1 = MaxSegmentLength,
                Value2 = MinSegmentLength,
                Value3 = Repeats,
                Value4 = (int)PlayMode,
                Value5 = (int)PitchedMode,
                Value6 = Frequency,
                Value7 = Rate,
                Value8 = NumSegments
            };


            return audioEffectParams;
        }

        public static void ProcessDSP(
            AudioEffectParameters audioEffectParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer,
            DynamicBuffer<DSPSampleBufferElement> dspBuffer)
        {
            var sampleIndexInSegment = 0; // Number of samples since last capture
            var crossingCount = 0;        // Number of rising zero-crossings since last capture
            float energySum = 0;          // Used to accumulate segment energy total

            const int minLength = 100;
            const int maxSegmentLength = 2000;

            int segmentDataStartIndex = sampleBuffer.Length - audioEffectParams.SampleTail - 1;

            var numSegments = (int)audioEffectParams.Value8;

            // NOTE: The DSP buffer size is extended by samples to include segment length and offset.
            float inputPrevious = 0;
            float unbiasedPrevious = 0;
            var segmentCurrent = 0;

            //-- RECORDING SECTION
            for (var i = 0; i < segmentDataStartIndex; i++)
            {
                float inputCurrent = sampleBuffer[i].Value;
                float unbiasedCurrent = inputCurrent - inputPrevious + unbiasedPrevious * 0.999997f;

                inputPrevious = inputCurrent;

                dspBuffer[i] = new DSPSampleBufferElement
                {
                    Value = unbiasedCurrent
                };

                energySum = energySum + unbiasedCurrent * unbiasedCurrent;

                // Is sample rising and crossing zero?
                if (DSPUtilsDOTS.IsCrossing(unbiasedCurrent, unbiasedPrevious))
                {
                    if (sampleIndexInSegment > maxSegmentLength)
                    {
                        crossingCount = 0;
                        sampleIndexInSegment = 0;
                    }
                    else
                    {
                        crossingCount++;

                        // If segment is complete
                        if (crossingCount > audioEffectParams.Value0 && sampleIndexInSegment >= minLength)
                        {
                            //-- Set length
                            SetSegmentData(0, segmentCurrent, sampleIndexInSegment);
                            //-- Set offset
                            SetSegmentData(1, segmentCurrent, i - sampleIndexInSegment);

                            //-- Reset counters, and increment segment
                            crossingCount = 0;
                            sampleIndexInSegment = 0;

                            segmentCurrent++;
                            segmentCurrent = (int)Mathf.Repeat(segmentCurrent, numSegments - 1);
                        }
                    }
                }
                else
                    sampleIndexInSegment++;

                unbiasedPrevious = unbiasedCurrent;
            }

            // Once the recording section is complete, update segment number to ACTUAL number of segements recorded
            numSegments = segmentCurrent;

            float rate = audioEffectParams.Value7;

            float playIndex = 0;
            var playSegment = 0;
            var playOffset = 0;
            int playLength = GetSegmentData(0, playSegment);

            //-- PLAYBACK SECTION
            for (var i = 0; i < segmentDataStartIndex; i++)
            {
                // Increase playback index by playback rate
                // Maintain default playback rate, Ascending pitch mode, Descending pitch mode, Pitched pitch mode
                playIndex += audioEffectParams.Value5 switch {
                    0 => rate,
                    1 => rate * Mathf.Max(1, playIndex / playLength),
                    2 => rate / Mathf.Max(1, Mathf.Pow(playIndex / playLength, 2)),
                    _ => audioEffectParams.Value6 * playLength / (audioEffectParams.SampleRate * audioEffectParams.Value0)
                };

                // Keep segment playback index within size of current segment
                float sampleInSegment = Mathf.Repeat(playIndex, playLength - 1);
                // Ensure samples are only read from the grain sample area of the DSP buffer
                var sampleToPlay = (int)Mathf.Repeat(playOffset + sampleInSegment, segmentDataStartIndex - 1);

                // Populate output sample with interpolated DSP sample
                sampleBuffer[i] = new GrainSampleBufferElement
                {
                    Value = Mathf.Lerp
                    (
                        sampleBuffer[i].Value,
                        DSPUtilsDOTS.LinearInterpolate(dspBuffer, sampleToPlay),
                        audioEffectParams.Mix
                    )
                };

                // Prepare phase for noise mode
                float phase = i / (dspBuffer.Length - audioEffectParams.SampleTail);

                //-- Switch to a new playback segment?
                if (!(playIndex >= playLength * Mathf.Floor(audioEffectParams.Value3))) continue;

                playIndex = sampleInSegment;

                // Forward, Reverse, Walk, Random
                playSegment += audioEffectParams.Value4 switch {
                    0 => 1,
                    1 => -1,
                    2 => (int)Mathf.PerlinNoise(phase, phase * 0.5f) * 2 - 1,
                    _ => 1 + (int)Mathf.Ceil((numSegments * Random.value + 1) / 2)
                };
                
                // Ensure new playback segment is within stored segments
                playSegment = (int)Mathf.Repeat(playSegment, numSegments - 1);
                // Get new lenght and offset for next playback sample
                playLength = GetSegmentData(0, playSegment);
                playOffset = GetSegmentData(1, playSegment);


                //Debug.Log("NEW SEGMENT: " + playSegment + "    NEW OFFSET: " + playOffset + "    NEW LENGTH: " + playLength);
                //playRMS = GetSegmentData(2, playSegment);
            }


            // Segment types are: 0 = Length, 1 = Offset
            void SetSegmentData(int type, int segment, int input)
            {
                dspBuffer[segmentDataStartIndex + type + segment * 2] = new DSPSampleBufferElement
                {
                    Value = input
                };
            }

            int GetSegmentData(int type, int segment)
            {
                return (int)dspBuffer[segmentDataStartIndex + type + segment * 2].Value;
            }
        }
    }
}