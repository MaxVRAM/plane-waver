using System;
using PlaneWaver.Modulation;
using Unity.Entities;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public class EmitterAuthRuntimeStates
    {
        public bool ObjectConstructed;
        public bool BaseInitialised;
        public bool EntityInitialised;
        private bool _isConnected;
        public bool IsPlaying { get; private set; }
        
        public EmitterAuthRuntimeStates()
        {
            ObjectConstructed = false;
            BaseInitialised = false;
            EntityInitialised = false;
            _isConnected = false;
            IsPlaying = false;
        }

        public bool IsInitialised()
        {
            return ObjectConstructed && BaseInitialised && EntityInitialised;
        }
        
        public bool SetConnected(bool connected)
        {
            if (IsInitialised()) return _isConnected = connected;

            IsPlaying = false;
            return _isConnected = false;

        }
        
        public bool IsReady()
        {
            return IsInitialised() && _isConnected;
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
        public float EmitterVolume;
        public float DynamicAmplitude;
        public ModulationComponent ModVolume;
        public ModulationComponent ModPlayhead;
        public ModulationComponent ModDuration;
        public ModulationComponent ModDensity;
        public ModulationComponent ModTranspose;
        public ModulationComponent ModLength;
    }

    public struct EmitterReadyTag : IComponentData { }

    public struct EmitterVolatileTag : IComponentData { }
}