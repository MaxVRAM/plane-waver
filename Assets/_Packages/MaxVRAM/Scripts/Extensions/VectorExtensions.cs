using UnityEngine;

namespace MaxVRAM.Extensions
{
    public static class VectorExtensions
    {
        public static Vector2 Clamp(this Vector2 vector, Vector2 range)
        {
            vector.x = Mathf.Clamp(vector.x, range.x, range.y);
            vector.y = Mathf.Clamp(vector.y, range.x, range.y);
            return vector;
        }

        public static Vector2 Clamp(this Vector2 vector, float min, float max)
        {
            vector = Clamp(vector, new Vector2(min, max));
            return vector;
        }

        public static Vector2 Sort(this Vector2 vector)
        {
            return vector.x > vector.y ? new Vector2(vector.y, vector.x) : vector;
        }
        
        public static Vector2 RoundDecimal(this Vector2 value, int decimalPlaces)
        {
            float multiplier = Mathf.Pow(10, decimalPlaces);
            return new Vector2(
                Mathf.Round(value.x * multiplier) / multiplier, 
                Mathf.Round(value.y * multiplier) / multiplier);
        }
        
        public static Vector3 RotatePointAroundPivot(this Vector3 point, Vector3 pivot, Vector3 angles)
        {
            // ref: https://answers.unity.com/questions/532297/rotate-a-vector-around-a-certain-point.html

            Vector3 dir = point - pivot; // get point direction relative to pivot
            dir = Quaternion.Euler(angles) * dir; // rotate it
            point = dir + pivot; // calculate rotated point
            return point; // return it
        }

        public static Vector3 RotateUpDirection(this Vector3 currentVector, float angle)
        {
            // ref: https://stackoverflow.com/questions/71710139/how-do-i-rotate-a-direction-vector3-upwards-by-an-angle-in-unity

            // if you know currentVector will always be normalized, can skip this step
            currentVector.Normalize();
            Vector3 axis = Vector3.Cross(currentVector, Vector3.up);
            // handle case where currentVector is colinear with up
            if (axis == Vector3.zero) axis = Vector3.right;

            return Quaternion.AngleAxis(angle, axis) * currentVector;
        }

        public static Vector3 RotateForwardDirection(this Vector3 currentVector, float angle)
        {
            // ref: https://stackoverflow.com/questions/71710139/how-do-i-rotate-a-direction-vector3-upwards-by-an-angle-in-unity

            // if you know currentVector will always be normalized, can skip this step
            currentVector.Normalize();
            Vector3 axis = Vector3.Cross(currentVector, Vector3.forward);
            // handle case where currentVector is colinear with up
            if (axis == Vector3.zero) axis = Vector3.up;

            return Quaternion.AngleAxis(angle, axis) * currentVector;
        }
    }
}