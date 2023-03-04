using System.Collections.Generic;
using PlaneWaver.Parameters;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    /// <summary>
    ///     Scriptable Object for storing deployable Stable Emitter configurations, which are then assigned to Frames.
    /// </summary>
    [CreateAssetMenu(fileName = "Emitter.Stable.", menuName = "PlaneWaver/Emitters/Stable", order = 1)]
    public class StableEmitterObject : EmitterObject
    {
        public StableEmitterObject()
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