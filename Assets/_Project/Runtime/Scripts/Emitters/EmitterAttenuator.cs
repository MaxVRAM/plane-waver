using System;
using UnityEngine;
using MaxVRAM.Audio;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public class EmitterAttenuator
    {
        public enum ConnectionFadeState
        {
            Disconnected, FadingIn, Connected
        }

        [Range(0f, 2f)] public float Gain;
        [Range(0f, 1f)] public float AudibleRange;
        [Range(0f, 0.5f)] public float AgeFadeIn;
        [Range(0.5f, 1f)] public float AgeFadeOut;

        public bool ReconnectionFade;
        [Range(0, 1)] public float ReconnectionFadeSeconds;
        private ConnectionFadeState _reconnectionState;

        private float _reconnectionTime;
        private float _reconnectionVolume;
        private float _distanceVolume;
        private float _listenerDistance;

        public EmitterAttenuator()
        {
            AudibleRange = 1f;
            AgeFadeIn = 0f;
            AgeFadeOut = 1f;
            ReconnectionFade = false;
            ReconnectionFadeSeconds = 0.1f;
        }

        public void UpdateConnectionState(bool isConnected)
        {
            if (!ReconnectionFade)
            {
                _reconnectionVolume = 1;
                return;
            }

            if (!isConnected)
            {
                _reconnectionState = ConnectionFadeState.Disconnected;
                _reconnectionVolume = 0;
                return;
            }

            if (_reconnectionState != ConnectionFadeState.Disconnected)
                return;

            _reconnectionVolume = 0;
            _reconnectionTime = Time.time;
            _reconnectionState = ConnectionFadeState.FadingIn;
        }

        public float CalculateAmplitude(ActorObject actor)
        {
            float outputVolume = UpdateReconnectionVolume();

            if (outputVolume == 0)
                return 0;

            outputVolume *= CalculateDistanceAmplitude(actor);
            outputVolume *= CalculateAgeAmplitude(actor);

            return outputVolume * Gain;
        }

        public float UpdateReconnectionVolume()
        {
            if (!ReconnectionFade)
                return _reconnectionVolume = 1;

            return _reconnectionState switch {
                ConnectionFadeState.Connected    => _reconnectionVolume = 1,
                ConnectionFadeState.Disconnected => _reconnectionVolume = 0,
                _                                => _reconnectionVolume = ProcessReconnectionFade()
            };
        }

        public float ProcessReconnectionFade()
        {
            float volume = Mathf.InverseLerp(0, ReconnectionFadeSeconds, Time.time - _reconnectionTime);

            if (_reconnectionVolume >= 1)
                _reconnectionState = ConnectionFadeState.Connected;

            return Mathf.Clamp01(volume);
        }

        public float CalculateDistanceAmplitude(ActorObject actor)
        {
            if (actor == null)
                return 1;

            _listenerDistance = actor.SpeakerTargetToListener();
            _distanceVolume = ScaleAmplitude.ListenerDistanceVolume
                    (_listenerDistance, SynthManager.Instance.ListenerRadius * AudibleRange);

            return _distanceVolume;
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