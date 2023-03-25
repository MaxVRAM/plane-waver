using UnityEngine;

using MaxVRAM.CustomGUI;

namespace PlaneWaver.Modulation
{
    public class FancyGUITester : MonoBehaviour
    {
        [FitDigits] public float FitDigits;
        
        [MirroredRange(-5000, -1.4f)] public Vector2 MirroredValue;
        
    }
}