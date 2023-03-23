using System.Collections.Generic;
using PlaneWaver.Modulation;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    /// <summary>
    ///     Scriptable Object for storing deployable Volatile Emitter configurations, which are then assigned to Frames.
    /// </summary>
    [CreateAssetMenu(fileName = "Emitter.Volatile.", menuName = "PlaneWaver/Emitters/Volatile", order = 1)]
    public class VolatileEmitterObject : BaseEmitterObject, IHasGUIContent
    {
        public GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Grain Emitter that spawns grains for a short, predetermined duration after being triggered.");
        }
        
        public VolatileEmitterObject()
        {
            Parameters = new List<Parameter> {
                new Volume(true),
                new Playhead(true),
                new Duration(true),
                new Density(true),
                new Transpose(true),
                new Length(true),
            };
        }
    }
}