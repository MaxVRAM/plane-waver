using UnityEngine;

namespace MaxVRAM.GUI
{
    public class MinMidMaxSliderAttribute : PropertyAttribute
    {
        public float min;
        public float mid;
        public float max;

        public MinMidMaxSliderAttribute(float min, float mid, float max)
        {
            this.min = min;
            this.mid = mid;
            this.max = max;
        }
    }

    public class BidirectionalSliderLockedAttribute : PropertyAttribute
    {
        public float min;
        public float max;
        public bool lockAtCentre;

        public BidirectionalSliderLockedAttribute(float min, float max, bool lockAtCentre = false)
        {
            this.min = min;
            this.max = max;
            this.lockAtCentre = lockAtCentre;
        }
    }
}
