using UnityEngine;
using Unity.Entities;


namespace PlaneWaver.DSP
{
    public class DSPClass : MonoBehaviour
    {
        public virtual AudioEffectParameters GetDSPBufferElement() { return new AudioEffectParameters(); }
    }
}