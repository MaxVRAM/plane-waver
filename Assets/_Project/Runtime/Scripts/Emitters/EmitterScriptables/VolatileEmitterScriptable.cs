using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PlaneWaver.Modulation;

namespace PlaneWaver.Emitters
{
    /// <summary>
    /// Scriptable Object for storing deployable Volatile Emitter configurations.
    /// Any number of Emitter objects can be assigned to Hosts, which interface Emitters with the Host's interaction Actors.
    /// </summary>
    [CreateAssetMenu(fileName = "Emitter.Volatile.", menuName = "Plane Waver/Emitters/Volatile", order = 1)]
    public class VolatileEmitterScriptable : BaseEmitterScriptable
    {
        #region CLASS DEFINITIONS
        
        public Parameter Length = new Parameter(ParamDefault.Length);
        
        #endregion
        
        #region INITIALISATION METHODS
        
        public override void InitialiseParameters()
        {
            base.InitialiseParameters();
            Length.Initialise(true);
        }

        #endregion
    }
}