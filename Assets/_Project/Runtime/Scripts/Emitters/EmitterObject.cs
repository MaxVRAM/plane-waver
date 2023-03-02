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

        public string Name;
        public string Description;
        public AudioObject AudioObject;

        protected int ParameterCount;
        public int GetParameterCount => ParameterCount;
        protected float[] PreviousSmoothed;
        protected bool IsInitialised;
        
        #endregion

        #region INITIALISATION METHODS
        
        public virtual void InitialiseParameters(in Actor actor) { }

        #endregion

        #region PARAMETER COMPONENT BUILDERS
        
        public virtual List<ModulationComponent> GetModulationComponents() { return null; }

        #endregion
    }
}
