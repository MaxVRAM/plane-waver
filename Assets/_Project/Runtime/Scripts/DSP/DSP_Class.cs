using UnityEngine;
using Unity.Entities;

public class DSP_Class : MonoBehaviour
{    
    public virtual AudioEffectParameters GetDSPBufferElement()
    {
        return new AudioEffectParameters();
    }
}