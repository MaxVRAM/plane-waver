using System.Collections.Generic;
using UnityEngine;

using PlaneWaver.Library;
using PlaneWaver.Modulation;

namespace PlaneWaver.Emitters
{
    public class BaseEmitterObject : ScriptableObject
    {
        public string EmitterName;
        public string Description;
        public AudioObject AudioObject;
        public List<Parameter> Parameters;
        
        
    }
}
