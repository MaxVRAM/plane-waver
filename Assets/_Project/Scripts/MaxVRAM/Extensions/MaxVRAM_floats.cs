using System;
using MaxVRAM.Extensions;

using Unity.Mathematics;
using UnityEngine;

namespace MaxVRAM.Extensions
{
    public static class ExtendFloats
    {
        public static bool IsInRange(this float value, bool inclusive = true)
        {
            if (inclusive)
                return value is >= 0f and <= 1f;
            else
                return value is > 0f and < 1f;
        }

        public static bool IsInRange(this float value, float min = 0f, float max = 1f, bool inclusive = true)
        {
            if (inclusive)
                return value >= min && value <= max;
            else
                return value > min && value < max;
        }

        public static bool IsInRange(this float value, float2 minMax, bool inclusive = true)
        {
            if (inclusive)
                return value >= minMax.x && value <= minMax.y;
            else
                return value > minMax.x && value < minMax.y;
        }

        public static float Mirrored(this float value, float mid)
        {
            float diff = value - mid;
            return diff > 0 ? mid - diff: mid + mid;
        }

        /// <summary>
        /// Replicates the functionality of Mathf.Abs() more efficently.
        /// </summary>
        public static float Abs(this float value)
        {
            return value > 0 ? value : -value;
        }

        /// <summary>
        /// Replicates the functionality of Mathf.PingPong() more efficently. Limited to normalised output range.
        /// </summary>
        public static float PingPongNorm(this float value)
        {
            value = value.Abs();
            value %= 2;
            if (value < 1)
                return value;
            else
                return 2 * 1 - value;
        }

        /// <summary>
        /// Applies a scaled PingPong-limited float to an abitrary normalised offset.
        /// </summary>
        /// <param name="amount">Float between -1 and 1. Negative values determine the direction of the PingPonged value.</param>
        /// <param name="offset">Float between 0. and 1. which offsets the resultant value. If amount = 0, return will always = offset.</param>
        /// <returns>A single normalised float between 0. and 1.</returns>
        public static float PingPongNorm(this float value, float amount, float offset)
        {
            value = value.Abs();
            float pinged = amount > 0 ? offset + value : offset - value;
            float ponged = Mathf.PingPong(pinged, 1f);
            return offset + (ponged - offset) * amount.Abs();
        }

        /// <summary>
        /// Replicates the functionality of Mathf.Repeat() more efficently. Limited to normalised output range.
        /// </summary>
        public static float RepeatNorm(this float value)
        {
            return value - Mathf.FloorToInt(value);
        }

        /// <summary>
        /// Applies a scaled Repeat-limited float to an abitrary normalised offset.
        /// </summary>
        /// <param name="amount">Float between -1 and 1. Negative values determine the direction of the Repeated value.</param>
        /// <param name="offset">Float between 0. and 1. which offsets the resultant value. If amount = 0, return will always = offset.</param>
        /// <returns>A single normalised float between 0. and 1.</returns>
        public static float RepeatNorm(this float value, float amount, float offset)
        {
            float reeh = amount > 0 ? offset + value : offset - value;
            float pete = reeh.RepeatNorm();
            return offset + (pete - offset) * amount.Abs();
        }


        public static Vector2 MakeMirroredVector(this float value, float mid)
        {
            float diff = Mathf.Abs(value - mid);
            return new Vector2(mid - diff, mid + diff);
        }
    }
}
