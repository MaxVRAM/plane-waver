using System;
using UnityEngine;

using PlaneWaver.Modulation;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public class StableEmitterAuth : BaseEmitterAuth
    {
        [SerializeReference] public StableEmitterObject StableEmitterAsset;
        
        public override bool InitialiseSubType()
        {
            Condition = PlaybackCondition.Constant;
            EmitterAsset = StableEmitterAsset;
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            Condition = PlaybackCondition.Constant;
            ReflectPlayheadAtLimit = true;
            AgeFadeOut = 0.95f;
        }
        
        public override bool RequestPlayback()
        {
            RuntimeState.SetPlaying
            (Condition switch {
                PlaybackCondition.Constant  => true,
                PlaybackCondition.Contact   => Actor.IsColliding,
                PlaybackCondition.Airborne  => !Actor.IsColliding,
                PlaybackCondition.Collision => throw new Exception("Stable emitters cannot use Collision condition."),
                _                           => throw new ArgumentOutOfRangeException()
            });
            return RuntimeState.IsPlaying;
        }
        
        public override EmitterComponent UpdateEmitterComponent(EmitterComponent previousData, int speakerIndex)
        {
            UpdateDSPEffectsBuffer();
            ModulationComponent[] modulations = EmitterAsset.GetModulationComponents();

            // data.LastSampleIndex = data.SpeakerIndex == speakerIndex ? data.LastSampleIndex : -1;
            // data.LastGrainDuration = data.SpeakerIndex == speakerIndex ? data.LastGrainDuration : -1;
            
            return new EmitterComponent {
                SpeakerIndex = speakerIndex,
                AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                LastSampleIndex = previousData.LastSampleIndex,
                LastGrainDuration = previousData.LastGrainDuration,
                SamplesUntilFade = int.MaxValue,  //_actor.Life.SamplesUntilFade(AgeFadeOut),
                SamplesUntilDeath = int.MaxValue, //_actor.Life.SamplesUntilDeath(),
                ReflectPlayhead = ReflectPlayheadAtLimit,
                EmitterVolume = VolumeAdjustment,
                DynamicAmplitude = DynamicAttenuation.CalculateAmplitude(Actor),
                ModVolume = modulations[0],
                ModPlayhead = modulations[1],
                ModDuration = modulations[2],
                ModDensity = modulations[3],
                ModTranspose = modulations[4]
            };
        }
    }
}
