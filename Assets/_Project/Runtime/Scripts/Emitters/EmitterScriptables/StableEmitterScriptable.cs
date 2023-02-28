using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    /// <summary>
    /// Scriptable Object for storing deployable Stable Emitter configurations.
    /// Any number of Emitter objects can be assigned to Hosts, which interface Emitters with the Host's interaction Actors.
    /// </summary>
    [CreateAssetMenu(fileName = "Emitter.Stable.", menuName = "Plane Waver/Emitters/Stable", order = 1)]
    public class StableEmitterScriptable : BaseEmitterScriptable
    {
    }
}