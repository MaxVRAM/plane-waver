using UnityEngine;

namespace MaxVRAM.CustomGUI
{
    public class ValueSliderAttribute : PropertyAttribute
    {
        public float Min { get; private set; }
        public float Max { get; private set; }
        public float MinDisplay { get; private set; }
        public float MaxDisplay { get; private set; }

        public ValueSliderAttribute(float minValue, float maxValue, float minDisplay, float maxDisplay)
        {
            Min = minValue;
            Max = maxValue;
            MinDisplay = minDisplay;
            MaxDisplay = maxDisplay;
        }
    }
}