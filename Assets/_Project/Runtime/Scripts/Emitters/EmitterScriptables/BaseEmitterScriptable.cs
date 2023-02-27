using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace PlaneWaver
{
    public class BaseEmitterScriptable : ScriptableObject
    {
        #region CLASS DEFINITIONS
        
        public enum EmitterType { Stable, Volatile }
        public EmitterType Type;
        private CollisionData _collisionData;
        private bool _isPlaying;
        
        
        
        #endregion

        #region INITIALISATION METHODS
        
        #endregion

        #region RUNTIME UPDATES

        #endregion
        
        #region EMITTER METHODS
        
        public void ArmCollisionEmitter(CollisionData collisionData)
        {
            if (Type != EmitterType.Volatile)
                return;
            _collisionData = collisionData;
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
    }
}
