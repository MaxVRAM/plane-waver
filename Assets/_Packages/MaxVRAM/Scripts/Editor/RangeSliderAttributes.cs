using System;
using UnityEngine;

namespace MaxVRAM.GUI
{
    public class RangeSliderAttribute : PropertyAttribute
    {
        public float Min;
        public float Max;
        public float MinDisplay;
        public float MaxDisplay;
        

        public RangeSliderAttribute(float min, float max, float minDisplay, float maxDisplay)
        {
            Min = min;
            Max = max;
            MinDisplay = minDisplay;
            MaxDisplay = maxDisplay;
        }
    }
}