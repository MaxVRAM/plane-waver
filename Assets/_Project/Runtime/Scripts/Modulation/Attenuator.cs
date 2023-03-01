using System;
using UnityEngine;

using PlaneWaver.Interaction;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class Attenuator
    {
        [Range(0f,1f)] public float DistanceFactor;
        [Range(0f,0.5f)] public float AgeFadeIn;
        [Range(0.5f,1f)] public float AgeFadeOut;
        public bool MuteOnDisconnection;
        private bool _muted;
        private float _reconnectionTimer;
        [Range(0,500)] public int ReconnectionFadeInMS;
        
        public Attenuator()
        {
            DistanceFactor = 1f;
            AgeFadeIn = 0f;
            AgeFadeOut = 1f;
            MuteOnDisconnection = true;
            _muted = false;
            _reconnectionTimer = 0;
            ReconnectionFadeInMS = 100;
        }

        public float CalculateAmplitudeMultiplier(bool connected, Actor actor)
        {
            float muteFade = CalculateMuting(connected);
            return _muted ? 0 : muteFade * CalculateDistanceAmplitude(actor) * CalculateAgeAmplitude(actor);
        }
        
        public float CalculateMuting(bool connected)
        {
            if (!MuteOnDisconnection)
            {
                _muted = false;
                return 1;
            }
            
            if (!connected)
            {
                _muted = true;
                return 0;
            }

            if (_muted)
            {
                _muted = false;
                _reconnectionTimer = ReconnectionFadeInMS;
                return 0;
            }

            if (_reconnectionTimer <= 0)
                return 1;
            
            _reconnectionTimer -= Time.deltaTime;
            float mutingAmplitude = Mathf.Clamp01(1 - _reconnectionTimer / ReconnectionFadeInMS);

            return mutingAmplitude;
        }
        
        public float CalculateDistanceAmplitude(Actor actor)
        {
            if (actor == null)
                return 1;
            
            return DistanceFactor * (1 - actor.DistanceFromListener()); 
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