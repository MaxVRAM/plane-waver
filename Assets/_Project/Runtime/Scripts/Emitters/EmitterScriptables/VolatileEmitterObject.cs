using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PlaneWaver.Modulation;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    /// <summary>
    /// Scriptable Object for storing deployable Volatile Emitter configurations.
    /// Any number of Emitter objects can be assigned to Hosts, which interface Emitters with the Host's interaction Actors.
    /// </summary>
    [CreateAssetMenu(fileName = "Emitter.Volatile.", menuName = "Plane Waver/Emitters/Volatile", order = 1)]
    public class VolatileEmitterObject : EmitterObject
    {
        #region CLASS DEFINITIONS
        
        public Parameter Length = new Parameter(ParamDefault.Length);
        
        #endregion
        
        #region INITIALISATION METHODS
        
        public override void InitialiseParameters(Actor actor)
        {
            base.InitialiseParameters(actor);
            
            Length.Initialise(true);
            ParameterCount = 6;
            PreviousSmoothedValues = new float[ParameterCount];
            for (var i = 0; i < ParameterCount; i++) PreviousSmoothedValues[i] = 0f;
            IsInitialised = true;
        }

        #endregion
        
        #region PARAMETER COMPONENT BUILDERS

        public override ModComponent[] BuildModulations(Actor actor)
        {
            if (!IsInitialised)
                throw new System.Exception("StableEmitter: Attempted to build ModComponents before initialisation.");

            var componentArray = new ModComponent[ParameterCount];

            
            return componentArray;
        }

        #endregion
    }
}