
using UnityEngine;

namespace MaxVRAM.Extensions
{
    public static class FloatExtensions
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

        public static bool IsInRange(this float value, Vector2 minMax, bool inclusive = true)
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
        
        public static float RoundDecimal(this float value, int decimalPlaces)
        {
            float multiplier = Mathf.Pow(10, decimalPlaces);
            return Mathf.Round(value * multiplier) / multiplier;
        }
        
        /// <summary>
        /// Provides standard Mathf.Lerp() functionality that snaps values closer than epsilon from the target.
        /// </summary>
        /// <param name="targetValue">New value for the current float to move towards.</param>
        /// <param name="t">Float between 0 and 1. 0 = no change, 1 = targetValue.</param>
        /// <param name="epsilon">Float that determines the threshold for snapping to the target value.</param>
        public static float Lerp(this float currentValue, float targetValue, float t, float epsilon = 0.001f)
        {
            if (t <= 0.0f)
                return currentValue;

            if (t >= 1.0f)
                return targetValue;

            float result = currentValue + (targetValue - currentValue) * t;

            if (Mathf.Abs(targetValue - result) <= epsilon)
                return targetValue;

            return result;
        }

        /// <summary>
        /// Provides linear interpolation from the current value to a target float. Uses Time.deltaTime internally
        /// and a "smoothing" parameter that inverts the behaviour of "t" in Mathf.Lerp.
        /// </summary>
        /// <param name="targetValue">Any float value destination for the current value to move towards.</param>
        /// <param name="smoothing">Float between 0 and 1. 0 = targetValue, 1 = no change.</param>
        /// <param name="epsilon">Float that determines the threshold for snapping to the target value.</param>
        /// <returns></returns>
        public static float Smooth(this float currentValue, float targetValue, float smoothing, float epsilon = 0.001f)
        {
            epsilon = epsilon <= Mathf.Epsilon ? Mathf.Epsilon : epsilon;

            if (smoothing > epsilon && Mathf.Abs(currentValue - targetValue) > epsilon)
                return Mathf.Lerp(currentValue, targetValue, (1 - smoothing) * 10f * Time.deltaTime);
            else
                return targetValue;
        }

        /// <summary>
        /// Replicates the functionality of Mathf.Abs() more efficiently.
        /// </summary>
        public static float Abs(this float value)
        {
            return value > 0 ? value : -value;
        }

        /// <summary>
        /// Replicates the functionality of Mathf.PingPong() more efficiently. Limited to normalised output range.
        /// </summary>
        public static float PingPongNorm(this float value)
        {
            value = value.Abs();
            value %= 2;
            if (value < 1)
                return value;
            return 2 * 1 - value;
        }

        /// <summary>
        /// Applies a scaled PingPong-limited float to an arbitrary normalised offset.
        /// </summary>
        /// <param name="amount">Float between -1 and 1. Negative values determine the direction of the PingPonged value.</param>
        /// <param name="offset">Float between 0. and 1. which offsets the resultant value. If amount = 0, return will always = offset.</param>
        /// <returns>A single normalised float between 0. and 1.</returns>
        public static float PingPongNorm(this float value, float amount, float offset)
        {
            value = value.Abs();
            float normalised = amount > 0 ? offset + value : offset - value;
            float pingponged = Mathf.PingPong(normalised, 1f);
            return offset + (pingponged - offset) * amount.Abs();
        }

        /// <summary>
        /// Replicates the functionality of Mathf.Repeat() more efficiently. Limited to normalised output range.
        /// </summary>
        public static float RepeatNorm(this float value)
        {
            return value - Mathf.FloorToInt(value);
        }

        /// <summary>
        /// Limits the value to a normalised range using a directional repeat limiter, to a value arbitrary normalised offset.
        /// </summary>
        /// <param name="amount">Float between -1 and 1. Negative values reverse the direction of the repeat loop.</param>
        /// <param name="offset">Float between 0 and 1 to offset the output value. Resultant value will always equal the offset when amount parameter is 0.</param>
        /// <returns>A normalised float between 0 and 1.</returns>
        public static float RepeatNorm(this float value, float amount, float offset)
        {
            float normalised = amount > 0 ? offset + value : offset - value;
            float repeated = normalised.RepeatNorm();
            return offset + (repeated - offset) * amount.Abs();
        }

        public static Vector2 MakeMirroredVector(this float value, float mid)
        {
            float diff = Mathf.Abs(value - mid);
            return new Vector2(mid - diff, mid + diff);
        }
    }
}
