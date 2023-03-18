using System;
using System.Collections.Generic;
using UnityEngine;

using PlaneWaver.Library;
using PlaneWaver.Modulation;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    public class BaseEmitterObject : ScriptableObject
    {
        #region CLASS DEFINITIONS

        public string EmitterName;
        public string Description;
        public AudioObject AudioObject;
        public List<Parameter> Parameters;
        public ModulationComponent[] ModulationComponents;

        public int GetParameterCount => Parameters.Count;
        private bool _isInitialised;
        
        #endregion

        #region INITIALISATION METHODS
        
        public void InitialiseParameters(in ActorObject actor)
        {
            foreach (Parameter parameter in Parameters) 
                parameter.Initialise(actor);
            ModulationComponents = new ModulationComponent[Parameters.Count];
            _isInitialised = true;   
        }
        
        #endregion

        #region PARAMETER COMPONENT BUILDERS

        public ModulationComponent[] GetModulationComponents()
        {
            if (!_isInitialised)
                throw new Exception("Emitter has not been initialised.");
            
            for (var i = 0; i < Parameters.Count; i++)
                ModulationComponents[i] = Parameters[i].CreateModulationComponent();

            return ModulationComponents;
        }

        #endregion
    }
}
