using System;
using UnityEngine;

using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public class VolatileEmitterAuth : BaseEmitterAuth
    {
        [SerializeReference] public VolatileEmitterObject VolatileEmitterAsset;

        public override bool InitialiseSubType()
        {
            Condition = PlaybackCondition.Collision;
            EmitterAsset = VolatileEmitterAsset;
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            Condition = PlaybackCondition.Collision;
            ReflectPlayhead = false;
        }

        public override void ApplyNewCollision(CollisionData collisionData)
        {
            base.ApplyNewCollision(collisionData);
            RuntimeState.SetPlaying(true);
        }
        
        public override bool IsPlaying()
        {
            if (Enabled)
                return RuntimeState.IsPlaying;

            RuntimeState.SetPlaying(false);
            return false;
        }

        public override EmitterComponent UpdateEmitterComponent(EmitterComponent emitter, int speakerIndex)
        {
            UpdateDSPEffectsBuffer();
            
            return new EmitterComponent {
                SpeakerIndex = speakerIndex,
                AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                LastSampleIndex = -1,
                LastGrainDuration = -1,
                SamplesUntilFade = -1,
                SamplesUntilDeath = -1,
                ReflectPlayhead = ReflectPlayhead,
                EmitterVolume = VolumeAdjustment,
                DynamicAmplitude = DynamicAttenuation.CalculateAmplitude(Actor),
                ModVolume = Parameters[0].GetModulationComponent(),
                ModPlayhead = Parameters[1].GetModulationComponent(),
                ModDuration = Parameters[2].GetModulationComponent(),
                ModDensity = Parameters[3].GetModulationComponent(),
                ModTranspose = Parameters[4].GetModulationComponent(),
                ModLength = Parameters[5].GetModulationComponent()
            };
        }
    }
}
