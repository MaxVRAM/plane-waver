using System;
using PlaneWaver.Modulation;
using Unity.Entities;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public class EmitterRuntimeStates
    {
        public bool ObjectConstructed;
        public bool BaseInitialised;
        public bool EntityInitialised;
        public bool IsConnected;
        public bool IsPlaying;
        
        public EmitterRuntimeStates()
        {
            ObjectConstructed = false;
            BaseInitialised = false;
            EntityInitialised = false;
            IsConnected = false;
            IsPlaying = false;
        }

        public bool IsInitialised()
        {
            return ObjectConstructed && BaseInitialised && EntityInitialised;
        }
        
        /// <summary>
        /// Set connected if bool parameter is true and emitter initialised.
        /// Otherwise, disables current playback state.
        /// </summary>
        /// <param name="connected">Connection bool state.</param>
        public void SetConnected(bool connected)
        {
            if (IsInitialised())
            {
                IsConnected = connected;
                return;
            }

            IsPlaying = false;
            IsConnected = false;
        }
        
        /// <summary>
        /// Returns true if emitter is initialised and connected.
        /// </summary>
        /// <returns>(bool) Ready state.</returns>
        public bool IsReady()
        {
            return IsInitialised() && IsConnected;
        }
        
        /// <summary>
        /// Updates playback state if emitter, defaulting to false if not ready.
        /// </summary>
        /// <param name="playing">(bool) New playing state to set emitter.</param>
        /// <returns>(bool) Returns new state.</returns>
        public bool SetPlaying(bool playing)
        {
            return IsPlaying = IsReady() && playing;
        }
    }
    
    public enum PlaybackCondition
    {
        Constant, Contact, Airborne, Collision
    }

    public struct EmitterComponent : IComponentData
    {
        public int SpeakerIndex;
        public int AudioClipIndex;
        public int LastSampleIndex;
        public int LastGrainDuration;
        public int SamplesUntilFade;
        public int SamplesUntilDeath;
        public bool ReflectPlayhead;
        public float Gain;
        public ParameterComponent ModVolume;
        public ParameterComponent ModPlayhead;
        public ParameterComponent ModDuration;
        public ParameterComponent ModDensity;
        public ParameterComponent ModTranspose;
        public ParameterComponent ModLength;
    }

    public struct EmitterReadyTag : IComponentData { }

    public struct EmitterVolatileTag : IComponentData { }
}