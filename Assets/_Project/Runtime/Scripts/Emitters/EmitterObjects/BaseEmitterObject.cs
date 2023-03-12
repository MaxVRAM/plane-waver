using System;
using System.Collections.Generic;
using System.Linq;
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

        public int GetParameterCount => Parameters.Count;
        private bool _isInitialised;
        
        #endregion

        #region INITIALISATION METHODS
        
        public void InitialiseParameters(in ActorObject actor)
        {
            foreach (Parameter parameter in Parameters) parameter.Initialise(actor);
            _isInitialised = true;   
        }
        
        #endregion

        #region PARAMETER COMPONENT BUILDERS

        public List<ModulationComponent> GetModulationComponents()
        {
            if (!_isInitialised)
                throw new Exception("Emitter has not been initialised.");
            
            return Parameters.Select(p => p.CreateModulationComponent()).ToList();
        }

        #endregion
    }
}
