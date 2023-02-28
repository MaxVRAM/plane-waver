using UnityEngine;

namespace PlaneWaver
{
    public static class EmitterParameterRanges
    {
        public static Vector2 Volume = new(0f, 2f);
        public static Vector2 Playhead = new(0f, 1f);
        public static Vector2 Length = new(10f, 1000f);
        public static Vector2 Duration = new(10f, 500f);
        public static Vector2 Density = new(1f, 10f);
        public static Vector2 Transpose = new(-3f, 3f);
    }
}