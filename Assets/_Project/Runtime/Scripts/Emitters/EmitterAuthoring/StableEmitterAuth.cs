using System;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public class StableEmitterAuth : BaseEmitterAuth
    {
        [SerializeReference] public StableEmitterObject StableEmitterAsset;
        
        public override bool InitialiseSubType()
        {
            Condition = Condition == PlaybackCondition.Collision ? PlaybackCondition.Constant : Condition;
            EmitterAsset = StableEmitterAsset;
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            Condition = PlaybackCondition.Constant;
            ReflectPlayhead = true;
        }
        
        public override bool IsPlaying()
        {
            RuntimeState.SetPlaying
            (Condition switch {
                PlaybackCondition.Constant  => true,
                PlaybackCondition.Contact   => Actor.IsColliding,
                PlaybackCondition.Airborne  => !Actor.IsColliding,
                PlaybackCondition.Collision => throw new Exception("Stable emitters cannot use Collision condition."),
                _                           => throw new ArgumentOutOfRangeException()
            });
            return RuntimeState.IsPlaying && Enabled;
        }
        
        public override EmitterComponent UpdateEmitterComponent(EmitterComponent emitter, int speakerIndex)
        {
            UpdateDSPEffectsBuffer();
            
            return new EmitterComponent {
                SpeakerIndex = speakerIndex,
                AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                LastSampleIndex = emitter.LastSampleIndex,
                LastGrainDuration = emitter.LastGrainDuration,
                SamplesUntilFade = int.MaxValue,  //_actor.Life.SamplesUntilFade(AgeFadeOut),
                SamplesUntilDeath = int.MaxValue, //_actor.Life.SamplesUntilDeath(),
                ReflectPlayhead = ReflectPlayhead,
                EmitterVolume = VolumeAdjustment,
                DynamicAmplitude = DynamicAttenuation.CalculateAmplitude(Actor),
                ModVolume = Parameters[0].GetModulationComponent(),
                ModPlayhead = Parameters[1].GetModulationComponent(),
                ModDuration = Parameters[2].GetModulationComponent(),
                ModDensity = Parameters[3].GetModulationComponent(),
                ModTranspose = Parameters[4].GetModulationComponent()
            };
        }
    }
}
