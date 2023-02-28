
using System.Collections.Generic;

using UnityEngine;

using PlaneWaver.Library;
using PlaneWaver.Modulation;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    public class EmitterObject : ScriptableObject
    {
        #region CLASS DEFINITIONS
        
        public AudioAsset AudioAsset;
        
        public Parameter Volume = new Parameter(ParamDefault.Volume);
        public Parameter Playhead = new Parameter(ParamDefault.Playhead);
        public Parameter Duration = new Parameter(ParamDefault.Duration);
        public Parameter Density = new Parameter(ParamDefault.Density);
        public Parameter Transpose = new Parameter(ParamDefault.Transpose);

        [Range(0f,1f)] public float AgeFadeOut = 0.95f;
        
        protected Actor ActorObject;
        protected int ParameterCount = 5;
        protected float[] PreviousSmoothedValues;
        protected bool IsInitialised;
        
        #endregion

        #region INITIALISATION METHODS
        
        public virtual void InitialiseParameters(Actor actor)
        {
            ActorObject = actor;
            Volume.Initialise(this is VolatileEmitterObject);
            Playhead.Initialise(this is VolatileEmitterObject);
            Duration.Initialise(this is VolatileEmitterObject);
            Density.Initialise(this is VolatileEmitterObject);
            Transpose.Initialise(this is VolatileEmitterObject);
        }

        #endregion

        #region PARAMETER COMPONENT BUILDERS

        public virtual ModComponent[] BuildModulations(Actor actor) { return null; }

        #endregion
    }
}
