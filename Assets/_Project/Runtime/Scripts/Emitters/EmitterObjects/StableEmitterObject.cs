using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PlaneWaver.Modulation;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    /// <summary>
    /// Scriptable Object for storing deployable Stable Emitter configurations, which are then assigned to Frames.
    /// </summary>
    [CreateAssetMenu(fileName = "Emitter.Stable.", menuName = "PlaneWaver/Emitters/Stable", order = 1)]
    public class StableEmitterObject : EmitterObject
    {
        #region INITIALISATION METHODS
        
        public override void InitialiseParameters()
        {
            base.InitialiseParameters();
            
            PreviousSmoothedValues = new float[ParameterCount];
            for (var i = 0; i < ParameterCount; i++) PreviousSmoothedValues[i] = 0f;
            IsInitialised = true;
        }

        #endregion
        
        #region PARAMETER COMPONENT BUILDERS

        public override ModComponent[] UpdateModulations(in Actor actor)
        {
            if (!IsInitialised)
                throw new System.Exception("StableEmitter: Attempted to build ModComponents before initialisation.");

            var componentArray = new ModComponent[ParameterCount];

            // TODO - implement modulation processing
            
            return componentArray;
        }

        #endregion
    }
}