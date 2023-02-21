using UnityEngine;

namespace MaxVRAM.Extensions
{
    public static class ExtendVectors
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
    }
}