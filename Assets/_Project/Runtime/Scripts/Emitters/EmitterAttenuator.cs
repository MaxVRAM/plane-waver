using System;
using PlaneWaver.Interaction;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class EmitterAttenuator
    {
        [Range(0f, 1f)] public float DistanceMultiplier;
        [Range(0f, 0.5f)] public float AgeFadeIn;
        [Range(0.5f, 1f)] public float AgeFadeOut;

        public bool MuteOnDisconnection;
        [Range(0, 500)] public int ReconnectionFadeInMS;
        private float _reconnectionTimer;
        private bool _muted;
        
        // Debug
        public float ReconnectionVolume;
        public float DistanceVolume;
        public float DistanceNorm;

        public EmitterAttenuator()
        {
            DistanceMultiplier = 1f;
            AgeFadeIn = 0f;
            AgeFadeOut = 1f;
            MuteOnDisconnection = true;
            ReconnectionFadeInMS = 100;
            _reconnectionTimer = ReconnectionFadeInMS;
            _muted = false;
        }
        
        public void UpdateConnectionState(bool isConnected)
        {
            if (!MuteOnDisconnection)
            {
                _muted = false;
                _reconnectionTimer = 0;
                ReconnectionVolume = 1;
                return;
            }

            if (!isConnected)
            {
                _muted = true;
                ReconnectionVolume = 0;
                return;
            }

            if (!_muted) return;

            _reconnectionTimer = 0;
            _muted = false;
        }

        public float CalculateAmplitude(ActorObject actor)
        {
            if (!MuteOnDisconnection || _muted)
                return ReconnectionVolume;

            ReconnectionVolume = CalculateReconnectionAmplitude();
            return ReconnectionVolume * CalculateDistanceAmplitude(actor) * CalculateAgeAmplitude(actor);
        }

        public float CalculateReconnectionAmplitude()
        {
            _reconnectionTimer += Time.deltaTime;
            
            if (_reconnectionTimer > ReconnectionFadeInMS)
                return 1;
            
            return Mathf.InverseLerp(0, ReconnectionFadeInMS, _reconnectionTimer);
        }

        public float CalculateDistanceAmplitude(ActorObject actor)
        {
            if (actor == null)
                return 1;
            DistanceNorm = 1 - actor.SpeakerTargetToListenerNorm();
            DistanceVolume = DistanceMultiplier * DistanceNorm;
            return DistanceVolume;
        }

        // TODO - Not implemented yet. Move these calculations to the Synthesis system for sample accuracy fades.
        public float CalculateAgeAmplitude(ActorObject actor)
        {
            // if (actor == null || actor.ActorLifeController.LiveForever)
            //     return 1;
            //
            // float age = actor.ActorLifeController.NormalisedAge();
            //
            // if (age < AgeFadeIn)
            //     return !Mathf.Approximately(AgeFadeIn, 0)
            //         ? Mathf.Clamp01(actor.ActorLifeController.NormalisedAge() / AgeFadeIn)
            //         : 1;
            //
            // if (age > AgeFadeOut)
            //     return !Mathf.Approximately(AgeFadeOut, 1)
            //         ? Mathf.Clamp01(1 - actor.ActorLifeController.NormalisedAge() / AgeFadeOut)
            //         : 1;
            //
            return 1;
        }
    }
}