using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public sealed class EmitterListWrapper<T>
    {
        public List<T> List;
    }
    public class EmitterList : MonoBehaviour
    {
        [SerializeReference] public EmitterListWrapper<BaseEmitterAuth> EmitterAuths;
    }
}