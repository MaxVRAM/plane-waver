using UnityEngine;
using Random = UnityEngine.Random;

namespace MaxVRAM
{
    public struct MaxMath
    {
        public static float ONE_DEGREE => 0.0027777777f;

        public static float Map(float val, float inMin, float inMax, float outMin, float outMax)
        {
            return (val - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;
        }
        public static float Map(float val, Vector2 inRange, float outMin, float outMax)
        {
            return (val - inRange.x) / (inRange.y - inRange.x) * (outMax - outMin) + outMin;
        }

        public static float Map(float val, float inMin, float inMax, float outMin, float outMax, float exp)
        {
            return Mathf.Pow((val - inMin) / (inMax - inMin), exp) * (outMax - outMin) + outMin;
        }

        public static float Smooth(float currentValue, float targetValue, float smoothing, float epsilon = 0.001f)
        {
            epsilon = epsilon <= Mathf.Epsilon ? Mathf.Epsilon : epsilon;

            if (smoothing > epsilon && Mathf.Abs(currentValue - targetValue) > epsilon)
                return Mathf.Lerp(currentValue, targetValue, (1 - smoothing) * 10f * Time.deltaTime);
            else
                return targetValue;
        }

        public static float InverseLerp(Vector2 range, float value)
        {
            return Mathf.InverseLerp(range.x, range.y, value);
        }

        public static float ScaleToNormNoClamp(float value, Vector2 range)
        {
            return range.x == range.y ? 0 : (value - range.x) / (range.y - range.x);
        }

        public static bool ClampCheck(ref float value, float min, float max)
        {
            if (value < min)
            {
                value = min;
                return true;
            }
            if (value > max)
            {
                value = max;
                return true;
            }
            return false;
        }

        public static bool IsInRange(float value, float min, float max)
        {
            return value >= min && value <= max;
        }

        public static bool IsInRange(float value, Vector2 range)
        {
            return value >= range.x && value <= range.y;
        }

        public static void SortFloats(ref float floatA, ref float floatB)
        {
            if (floatA > floatB)
            {
                (floatB, floatA) = (floatA, floatB);
            }
        }

        public static float FadeInOut(float norm, float inEnd, float outStart)
        {
            norm = Mathf.Clamp01(norm);
            float fade = 1;

            if (inEnd != 0 && norm < inEnd)
                fade = norm / inEnd;

            if (outStart != 1 && norm > outStart)
                fade = (1 - norm) / (1 - outStart);

            return fade;
        }

        public static float FadeInOut(float normPosition, float inOutPoint)
        {
            return FadeInOut(normPosition, inOutPoint, 1 - inOutPoint);
        }

        public static void SphericalToCartesian(float radius, float polar, float elevation, out Vector3 outCart)
        {
            // https://blog.nobel-joergensen.com/2010/10/22/spherical-coordinates-in-unity/
            float a = radius * Mathf.Cos(elevation);
            outCart.x = a * Mathf.Cos(polar);
            outCart.y = radius * Mathf.Sin(elevation);
            outCart.z = a * Mathf.Sin(polar);
        }

        public static void CartesianToSpherical(Vector3 cartCoords, out float outRadius, out float outPolar, out float outElevation)
        {
            // https://blog.nobel-joergensen.com/2010/10/22/spherical-coordinates-in-unity/
            if (cartCoords.x == 0)
                cartCoords.x = Mathf.Epsilon;
            outRadius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                            + (cartCoords.y * cartCoords.y)
                            + (cartCoords.z * cartCoords.z));
            outPolar = Mathf.Atan(cartCoords.z / cartCoords.x);
            if (cartCoords.x < 0)
                outPolar += Mathf.PI;
            outElevation = Mathf.Asin(cartCoords.y / outRadius);
        }

        public class SphericalCoords
        {
            private float _Radius;
            private float _Polar;
            private float _Elevation;
            public float Radius { get { return _Radius; } }
            public float Polar { get { return _Polar; } }
            public float Elevation { get { return _Elevation; } }

            public SphericalCoords(Vector3 cartesianCoords)
            {
                CartesianToSpherical(cartesianCoords, out _Radius, out _Polar, out _Elevation);
            }
            public SphericalCoords(SphericalCoords sphericalCoords)
            {
                _Radius = sphericalCoords._Radius;
                _Polar = sphericalCoords._Polar;
                _Elevation = sphericalCoords._Elevation;
            }

            public Vector3 GetAsVector() { return new Vector3(_Radius, _Polar, _Elevation); }
            public void GetAsVector(out Vector3 sphericalCoods) { sphericalCoods = GetAsVector(); }

            public SphericalCoords FromCartesian(Vector3 cartesianCoords)
            {
                CartesianToSpherical(cartesianCoords, out _Radius, out _Polar, out _Elevation);
                return this;
            }

            public Vector3 ToCartesian()
            {
                SphericalToCartesian(_Radius, _Polar, _Elevation, out Vector3 cartesianCoords);
                return cartesianCoords;
            }
        }

        public static float TangentalSpeedFromQuaternion(Quaternion quat)
        {
            float angleInDegrees;
            Vector3 rotationAxis;
            quat.ToAngleAxis(out angleInDegrees, out rotationAxis);
            Vector3 angularDisplacement = rotationAxis * angleInDegrees * Mathf.Deg2Rad;
            Vector3 angularSpeed = angularDisplacement / Time.deltaTime;
            return angularSpeed.magnitude;
        }
    }

    public struct Rando
    {
        public static float Range(Vector2 range) { return Random.Range(range.x, range.y); }
        public static int PickOne(int[] selection) { return selection[Random.Range(0, selection.Length)]; }

        public static int[] RandomIntList(int size = 10, int min = 0, int max = 100) 
        { 
            int[] outputArray = new int[size];
            for (int i = 0; i < size; i++) { outputArray[i] = Random.Range(min, max); }
            return outputArray;
        }
    }
}