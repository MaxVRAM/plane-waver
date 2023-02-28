using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PlaneWaver.Modulation;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    /// <summary>
    /// Scriptable Object for storing deployable Stable Emitter configurations.
    /// Any number of Emitter objects can be assigned to Hosts, which interface Emitters with the Host's interaction Actors.
    /// </summary>
    [CreateAssetMenu(fileName = "Emitter.Stable.", menuName = "Plane Waver/Emitters/Stable", order = 1)]
    public class StableEmitterObject : EmitterObject
    {
        #region INITIALISATION METHODS
        
        public override void InitialiseParameters(Actor actor)
        {
            base.InitialiseParameters(actor);
            
            PreviousSmoothedValues = new float[ParameterCount];
            for (var i = 0; i < ParameterCount; i++) PreviousSmoothedValues[i] = 0f;
            IsInitialised = true;
        }

        #endregion
        
        #region PARAMETER COMPONENT BUILDERS

        public override IEnumerable<ModComponent> BuildModulations(Actor actor)
        {
            if (!IsInitialised)
                throw new System.Exception("StableEmitter: Attempted to build ModComponents before initialisation.");

            var componentArray = new ModComponent[ParameterCount];

            
            return componentArray;
        }

        #endregion
    }
}