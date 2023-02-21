using UnityEngine;

namespace PlaneWaver
{
    public static class EmitterParameterRanges
    {
        public static Vector2 _Volume = new(0f, 2f);
        public static Vector2 _Length = new(10f, 1000f);
        public static Vector2 _Playhead = new(0f, 1f);
        public static Vector2 _Duration = new(10f, 500f);
        public static Vector2 _Density = new(1f, 10f);
        public static Vector2 _Transpose = new(-3f, 3f);
    }
}