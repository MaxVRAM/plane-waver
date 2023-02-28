
using UnityEngine;

using PlaneWaver.Modulation;

namespace PlaneWaver.Emitters
{
    public class BaseEmitterScriptable : ScriptableObject
    {
        #region CLASS DEFINITIONS
        
        public AudioAssetScriptable AudioAsset;
        
        public Parameter Volume = new Parameter(ParamDefault.Volume);
        public Parameter Playhead = new Parameter(ParamDefault.Playhead);
        public Parameter Duration = new Parameter(ParamDefault.Duration);
        public Parameter Density = new Parameter(ParamDefault.Density);
        public Parameter Transpose = new Parameter(ParamDefault.Transpose);
        
        #endregion

        #region INITIALISATION METHODS
        
        public virtual void InitialiseParameters()
        {
            Volume.Initialise(this is VolatileEmitterScriptable);
            Playhead.Initialise(this is VolatileEmitterScriptable);
            Duration.Initialise(this is VolatileEmitterScriptable);
            Density.Initialise(this is VolatileEmitterScriptable);
            Transpose.Initialise(this is VolatileEmitterScriptable);
        }

        #endregion
    }
}
