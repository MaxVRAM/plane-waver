using System;
using System.Collections.Generic;
using System.Linq;
using MaxVRAM.GUI;
using UnityEngine;

using PlaneWaver.Library;
using PlaneWaver.Parameters;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    public class EmitterObject : ScriptableObject
    {
        #region CLASS DEFINITIONS

        [RangeSlider(0, 1, -10, 10)]
        public Vector2 TestVector;
        public string EmitterName;
        public string Description;
        public AudioObject AudioObject;
        public List<Parameter> Parameters;

        public int GetParameterCount => Parameters.Count;
        private bool _isInitialised;
        
        #endregion

        #region INITIALISATION METHODS
        
        public void InitialiseParameters(in Actor actor)
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
