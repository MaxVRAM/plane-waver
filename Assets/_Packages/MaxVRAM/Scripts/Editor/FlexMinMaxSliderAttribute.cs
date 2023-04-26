// https://frarees.github.io/default-gist-license

using System;
using UnityEngine;

namespace MaxVRAM.CustomGUI
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class FlexMinMaxSliderAttribute : PropertyAttribute
    {
        public float Min { get; set; }
        public float Max { get; set; }
        public bool DataFields { get; set; } = true;
        public bool FlexibleFields { get; set; } = true;
        public bool Bound { get; set; } = true;
        public bool Round { get; set; } = true;

        public FlexMinMaxSliderAttribute() : this(0, 1) { }

        public FlexMinMaxSliderAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
