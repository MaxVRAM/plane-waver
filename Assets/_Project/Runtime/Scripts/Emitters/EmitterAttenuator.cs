using System;
using PlaneWaver.Interaction;
using UnityEngine;

namespace PlaneWaver.Parameters
{
    [Serializable]
    public class EmitterAttenuator
    {
        [Range(0f, 1f)] public float DistanceFactor;
        [Range(0f, 0.5f)] public float AgeFadeIn;
        [Range(0.5f, 1f)] public float AgeFadeOut;

        public bool MuteOnDisconnection;
        [Range(0, 500)] public int ReconnectionFadeInMS;
        private float _reconnectionTimer;
        public bool Muted { get; set; }
        
        // Debug
        public float MuteVolume;
        public float DistanceVolume;
        public float DistanceNorm;

        public EmitterAttenuator()
        {
            DistanceFactor = 1f;
            AgeFadeIn = 0f;
            AgeFadeOut = 1f;
            MuteOnDisconnection = true;
            Muted = false;
            _reconnectionTimer = 0;
            ReconnectionFadeInMS = 100;
        }

        public float CalculateAmplitudeMultiplier(bool connected, Actor actor)
        {
            return CalculateMuting(connected, out MuteVolume)
                    ? MuteVolume
                    : MuteVolume * CalculateDistanceAmplitude(actor) * CalculateAgeAmplitude(actor);
        }

        public bool CalculateMuting(bool connected, out float muteFade)
        {
            if (!MuteOnDisconnection)
            {
                muteFade = 1;
                return Muted = false;
            }

            if (!connected)
            {
                muteFade = 0;
                return Muted = true;
            }

            if (Muted)
            {
                _reconnectionTimer = ReconnectionFadeInMS;
                muteFade = 0;
                return Muted = false;
            }

            if (_reconnectionTimer <= 0)
            {
                muteFade = 1;
                return Muted = false;
            }

            _reconnectionTimer -= Time.deltaTime;
            muteFade = Mathf.Clamp01(1 - _reconnectionTimer / ReconnectionFadeInMS);
            return Muted = false;
        }

        public float CalculateDistanceAmplitude(Actor actor)
        {
            if (actor == null)
                return 1;
            DistanceNorm = 1 - actor.SpeakerTargetToListenerNorm();
            DistanceVolume = DistanceFactor * DistanceNorm;
            return DistanceVolume;
        }

        // TODO - Not implemented yet. Move these calculations to the Synthesis system for sample accuracy fades.
        public float CalculateAgeAmplitude(Actor actor)
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