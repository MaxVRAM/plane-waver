using System.Collections.Generic;
using PlaneWaver.Modulation;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    /// <summary>
    ///     Scriptable Object for storing deployable Stable Emitter configurations, which are then assigned to Frames.
    /// </summary>
    [CreateAssetMenu(fileName = "emit_Stable", menuName = "PlaneWaver/Emitters/Stable", order = 1)]
    public class StableEmitterObject : BaseEmitterObject, IHasGUIContent
    {
        public StableEmitterObject()
        {
            Parameters = new List<Parameter> {
                new(ParameterType.Volume, false),
                new(ParameterType.Playhead, false),
                new(ParameterType.Duration, false),
                new(ParameterType.Density, false),
                new(ParameterType.Transpose, false)
            };
        }

        public GUIContent GetGUIContent()
        {
            return new GUIContent
            (IconManager.GetIcon(this),
                "Grain Emitter that continues producing grains while its condition evaluates as true.");
        }
    }
}