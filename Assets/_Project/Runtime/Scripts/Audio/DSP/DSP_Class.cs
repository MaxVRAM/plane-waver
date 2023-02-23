using UnityEngine;
using Unity.Entities;

public class DSP_Class : MonoBehaviour
{    
    public virtual DSPParametersElement GetDSPBufferElement()
    {
        return new DSPParametersElement();
    }
}