using System;
using System.Collections.Generic;
using UnityEngine;

using PlaneWaver.Interaction;
using PlaneWaver.Modulation;

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
            ReflectPlayheadAtLimit = false;
            AgeFadeOut = 1;
        }

        public override bool RequestPlayback()
        {
            // Triggered Volatile Emitters set playback to false once they have received a playback request.
            return RuntimeState.IsPlaying && RuntimeState.SetPlaying(false);
        }

        public override void ApplyNewCollision(CollisionData collisionData)
        {
            base.ApplyNewCollision(collisionData);
            RuntimeState.SetPlaying(true);
        }

        public override EmitterComponent UpdateEmitterComponent(EmitterComponent previousData, int speakerIndex)
        {
            UpdateDSPEffectsBuffer();
            ModulationComponent[] modulations = EmitterAsset.GetModulationComponents();
            
            return new EmitterComponent {
                SpeakerIndex = speakerIndex,
                AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                LastSampleIndex = -1,
                LastGrainDuration = -1,
                SamplesUntilFade = -1,
                SamplesUntilDeath = -1,
                ReflectPlayhead = ReflectPlayheadAtLimit,
                EmitterVolume = VolumeAdjustment,
                DynamicAmplitude = DynamicAttenuation.CalculateAmplitude(Actor),
                ModVolume = modulations[0],
                ModPlayhead = modulations[1],
                ModDuration = modulations[2],
                ModDensity = modulations[3],
                ModTranspose = modulations[4],
                ModLength = modulations[5]
            };
        }
    }
}
