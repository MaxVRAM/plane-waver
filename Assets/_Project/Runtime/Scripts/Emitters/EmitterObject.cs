using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using PlaneWaver.Library;
using PlaneWaver.Parameters;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    public class EmitterObject : ScriptableObject
    {
        #region CLASS DEFINITIONS

        public string EmitterName;
        public string Description;
        public AudioObject AudioObject;
        public List<Parameter> Parameters;

        public int GetParameterCount => Parameters.Count;
        protected bool IsInitialised;
        
        #endregion

        #region INITIALISATION METHODS
        
        public void InitialiseParameters(in Actor actor)
        {
            foreach (Parameter parameter in Parameters) parameter.Initialise(actor);
            IsInitialised = true;
        }
        
        #endregion

        #region PARAMETER COMPONENT BUILDERS

        public List<ModulationComponent> GetModulationComponents()
        {
            if (!IsInitialised)
                throw new Exception("Emitter has not been initialised.");
            
            return Parameters.Select(p => p.CreateModulationComponent()).ToList();
        }

        #endregion
    }
}
