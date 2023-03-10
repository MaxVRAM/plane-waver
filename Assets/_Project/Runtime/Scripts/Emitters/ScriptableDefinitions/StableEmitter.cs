using System.Collections.Generic;
using PlaneWaver.Modulation;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    /// <summary>
    ///     Scriptable Object for storing deployable Stable Emitter configurations, which are then assigned to Frames.
    /// </summary>
    [CreateAssetMenu(fileName = "Emitter.Stable.", menuName = "PlaneWaver/Emitters/Stable", order = 1)]
    public class StableEmitter : BaseEmitterObject, IHasGUIContent
    {
        public GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Grain Emitter that continues producing grains while its condition evaluates as true.");
        }
        
        public StableEmitter()
        {
            Parameters = new List<Parameter> {
                new Volume(),
                new Playhead(),
                new Duration(),
                new Density(),
                new Transpose()
            };
        }
    }
}