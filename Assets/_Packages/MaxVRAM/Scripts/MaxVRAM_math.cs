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
            return Map(val, inRange.x, inRange.y, outMin, outMax);
        }

        public static float Map(float val, Vector2 inRange, Vector2 outRange)
        {
            return Map(val, inRange.x, inRange.y, outRange.x, outRange.y);
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

        public static void SphericalToCartesian(float radius, float polar, float elevation, out Vector3 cartesianCoords)
        {
            // https://blog.nobel-joergensen.com/2010/10/22/spherical-coordinates-in-unity/
            float a = radius * Mathf.Cos(elevation);
            cartesianCoords.x = a * Mathf.Cos(polar);
            cartesianCoords.y = radius * Mathf.Sin(elevation);
            cartesianCoords.z = a * Mathf.Sin(polar);
        }
        
        public static Vector3 SphericalToCartesian(float radius, float polar, float elevation)
        {
            SphericalToCartesian(radius, polar, elevation, out Vector3 outCart);
            return outCart;
        }
        
        public static Vector3 SphericalToCartesian(SphericalCoordinates sphericalCoordinates)
        {
            SphericalToCartesian(sphericalCoordinates.Radius, sphericalCoordinates.Polar, sphericalCoordinates.Elevation, out Vector3 outCart);
            return outCart;
        }

        public static void CartesianToSpherical(Vector3 cartesianCoords, out float outRadius, out float outPolar, out float outElevation)
        {
            // https://blog.nobel-joergensen.com/2010/10/22/spherical-coordinates-in-unity/
            if (cartesianCoords.x == 0)
                cartesianCoords.x = Mathf.Epsilon;
            outRadius = Mathf.Sqrt((cartesianCoords.x * cartesianCoords.x)
                            + (cartesianCoords.y * cartesianCoords.y)
                            + (cartesianCoords.z * cartesianCoords.z));
            outPolar = Mathf.Atan(cartesianCoords.z / cartesianCoords.x);
            if (cartesianCoords.x < 0)
                outPolar += Mathf.PI;
            outElevation = Mathf.Asin(cartesianCoords.y / outRadius);
        }
        
        public static SphericalCoordinates CartesianToSpherical(Vector3 cartesianCoords)
        {
            CartesianToSpherical(cartesianCoords, out float outRadius, out float outPolar, out float outElevation);
            return new SphericalCoordinates(outRadius, outPolar, outElevation);
        }

        public struct SphericalCoordinates
        {
            private float _radius;
            private float _polar;
            private float _elevation;
            public float Radius { get => _radius; set => _radius = value; }
            public float Polar { get => _polar; set => _polar = value; }
            public float Elevation { get => _elevation; set => _elevation = value; }
            
            public SphericalCoordinates(float radius, float polar, float elevation)
            {
                _radius = radius;
                _polar = polar;
                _elevation = elevation;
            }
            
            public SphericalCoordinates(Vector3 cartesianCoords)
            {
                CartesianToSpherical(cartesianCoords, out _radius, out _polar, out _elevation);
            }
            
            public Vector3 ToCartesian()
            {
                SphericalToCartesian(_radius, _polar, _elevation, out Vector3 cartesianCoords);
                return cartesianCoords;
            }
        }

        public static float TangentalSpeedFromQuaternion(Quaternion quaternion)
        {
            quaternion.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);
            Vector3 angularDisplacement = rotationAxis * (angleInDegrees * Mathf.Deg2Rad);
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
            var outputArray = new int[size];
            for (var i = 0; i < size; i++) { outputArray[i] = Random.Range(min, max); }
            return outputArray;
        }
    }
}