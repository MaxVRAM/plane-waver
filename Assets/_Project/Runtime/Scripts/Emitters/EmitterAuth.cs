using System;
using Unity.Entities;

using PlaneWaver.Modulation;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public class EmitterAuth
    {
        public EmitterPlaybackType PlaybackType;
        public BaseEmitterScriptable EmitterAsset;
        private CollisionData _collisionData;
        private bool _initialised;
        private bool _isPlaying;
        public bool IsVolatile => EmitterAsset is VolatileEmitterScriptable;
        
        
        public void Initialise()
        {
            if (_initialised)
                return;
            
            if (EmitterAsset == null)
                throw new Exception("EmitterAuth: EmitterAsset is null.");
            
            if (EmitterAsset.AudioAsset == null)
                throw new Exception("EmitterAuth: EmitterAsset.AudioAsset is null.");

            if (PlaybackType == EmitterPlaybackType.Volatile && !IsVolatile)
                throw new Exception("EmitterAuth: PlaybackType is Volatile but EmitterAsset is not Volatile.");

            if (PlaybackType != EmitterPlaybackType.Volatile && IsVolatile)
                throw new Exception("EmitterAuth: PlaybackType is not Volatile but EmitterAsset is Volatile.");
            
            EmitterAsset.InitialiseParameters();
            _initialised = true;
        }
        
        
        #region STATE CONTROL METHODS
        
        public void ApplyNewCollision(CollisionData collisionData)
        {
            _collisionData = collisionData;
            
            if (!IsVolatile || !_initialised)
                return;

            _isPlaying = true;
        }
        
        #endregion
        
    }
    
    public struct EmitterComponent : IComponentData
    {
        public bool PingPong;
        public int SpeakerIndex;
        public int AudioClipIndex;
        public int LastSampleIndex;
        public int SamplesUntilFade;
        public int SamplesUntilDeath;
        public int PreviousGrainDuration;
        public float AmplitudeMultiplier;
        public ModComponent Volume;
        public ModComponent Playhead;
        public ModComponent Duration;
        public ModComponent Density;
        public ModComponent Transpose;
        public ModComponent Length;
    }
    
    public struct EmitterPlayingTag : IComponentData { }
    public struct EmitterVolatileTag : IComponentData { }
    public struct EmitterPingPongTag : IComponentData { }
}