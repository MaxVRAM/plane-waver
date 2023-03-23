using System;
using UnityEngine;

using MaxVRAM.Audio;
using PlaneWaver.Interaction;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class EmitterAttenuator
    {
        public enum ConnectionFadeState
        {
            AlwaysFull = 0,
            Disconnected = 1,
            FadingIn = 2,
            Connected = 3
        }
        
        [Range(0f, 1f)] public float RadiusMultiplier;
        [Range(0f, 0.5f)] public float AgeFadeIn;
        [Range(0.5f, 1f)] public float AgeFadeOut;

        public ConnectionFadeState ConnectionFade;
        [Range(0, 1)] public float ConnectFadeIn;
        public float ReconnectionTime;
        
        public float ReconnectionVolume;
        public float DistanceVolume;
        public float ListenerDistance;

        public EmitterAttenuator()
        {
            RadiusMultiplier = 1f;
            AgeFadeIn = 0f;
            AgeFadeOut = 1f;
            ConnectionFade = ConnectionFadeState.AlwaysFull;
            ConnectFadeIn = 0.1f;
        }
        
        public void UpdateConnectionState(bool isConnected)
        {
            if (ConnectionFade == ConnectionFadeState.AlwaysFull)
            {
                ReconnectionVolume = 1;
                return;
            }

            if (!isConnected)
            {
                ConnectionFade = ConnectionFadeState.Disconnected;
                ReconnectionVolume = 0;
                return;
            }

            if (ConnectionFade != ConnectionFadeState.Disconnected)
                return;
            
            ReconnectionVolume = 0;
            ReconnectionTime = Time.time;
            ConnectionFade = ConnectionFadeState.FadingIn;
        }

        public float CalculateAmplitude(ActorObject actor)
        {
            float outputVolume = UpdateReconnectionVolume();

            if (outputVolume == 0)
                return 0;

            outputVolume *= CalculateDistanceAmplitude(actor);
            outputVolume *= CalculateAgeAmplitude(actor);
            
            return outputVolume;
        }

        public float UpdateReconnectionVolume()
        {
            if (ConnectionFade is not ConnectionFadeState.FadingIn)
                ReconnectionVolume = ConnectionFade is ConnectionFadeState.Disconnected ? 0 : 1;
            
            ReconnectionVolume = Mathf.InverseLerp(0, ConnectFadeIn, Time.time - ReconnectionTime);

            if (ReconnectionVolume < 1)
                return ReconnectionVolume;

            ConnectionFade = ConnectionFadeState.Connected;

            return ReconnectionVolume = 1;
        }

        public float CalculateDistanceAmplitude(ActorObject actor)
        {
            if (actor == null)
                return 1;

            ListenerDistance = actor.SpeakerTargetToListener();
            DistanceVolume = ScaleAmplitude.ListenerDistanceVolume(
                ListenerDistance,
                SynthManager.Instance.ListenerRadius * RadiusMultiplier);
            return DistanceVolume;
        }

        // TODO - Not implemented yet. Move these calculations to the Synthesis system for sample accuracy fades.
        public float CalculateAgeAmplitude(ActorObject actor)
        {
            if (actor == null || actor.Controller.LiveForever)
                return 1;
            
            float age = actor.Controller.NormalisedAge();
            
            if (age < AgeFadeIn)
                return Mathf.Clamp01(age / AgeFadeIn);
            
            if (age > AgeFadeOut)
                return 1 - Mathf.Clamp01((age - AgeFadeOut) / AgeFadeOut);
            
            return 1;
        }
    }
}
