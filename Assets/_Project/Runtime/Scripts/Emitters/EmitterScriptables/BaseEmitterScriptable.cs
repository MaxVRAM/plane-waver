using System.Collections;
using System.Collections.Generic;

using Unity.Entities;
using UnityEngine;

using MaxVRAM.Noise;
using PlaneWaver.Modulation;

namespace PlaneWaver
{
    public class BaseEmitterScriptable : ScriptableObject
    {
        #region RUNTIME FIELDS

        private PerlinNoise.Array _perlinArray = new (7, 1f);
        private CollisionData _collisionData;
        private bool _isPlaying;
        
        #endregion

        #region CLASS DEFINITIONS
        
        public enum EmitterType { Stable, Volatile }
        public EmitterType DefinedEmitterType;
        public bool IsVolatile => DefinedEmitterType == EmitterType.Volatile;
        
        public Parameter Volume = new Parameter(ParamDefaults.Volume);
        public Parameter Playhead = new Parameter(ParamDefaults.Playhead);
        public Parameter Duration = new Parameter(ParamDefaults.Duration);
        public Parameter Density = new Parameter(ParamDefaults.Density);
        public Parameter Transpose = new Parameter(ParamDefaults.Transpose);
        public Parameter Length = new Parameter(ParamDefaults.Length);
        
        #endregion

        #region INITIALISATION METHODS
        
        public void InitialiseParameters()
        {
            Volume.Initialise(IsVolatile);
            Playhead.Initialise(IsVolatile);
            Duration.Initialise(IsVolatile);
            Density.Initialise(IsVolatile);
            Transpose.Initialise(IsVolatile);
            Length.Initialise(IsVolatile);
        }
        
        #endregion

        #region RUNTIME UPDATES

        #endregion
        
        #region EMITTER METHODS
        
        public void ArmCollisionEmitter(CollisionData collisionData)
        {
            if (DefinedEmitterType != EmitterType.Volatile)
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
