using UnityEngine;

namespace PlaneWaver.Modulation
{
    public struct Defaults
    {
        public readonly int Index;
        public readonly string Name;
        public readonly Vector2 Range;
        public readonly Vector2 DefaultRange;
        public readonly bool VolatileOnly;
        public readonly bool FixedStart;
        public readonly bool FixedEnd;

        public Defaults(
            int index, string name, Vector2 range, Vector2 defaultRange, bool volatileOnly, bool fixedStart,
            bool fixedEnd)
        {
            Index = index;
            Name = name;
            Range = range;
            DefaultRange = defaultRange;
            VolatileOnly = volatileOnly;
            FixedStart = fixedStart;
            FixedEnd = fixedEnd;
        }

        public static readonly Defaults Volume = new(
            0,
            "Volume",
            new Vector2(0f, 2f),
            new Vector2(1, 0),
            false,
            false,
            true
        );
        public static readonly Defaults Playhead = new(
            1,
            "Playhead",
            new Vector2(0f, 1f),
            new Vector2(0, 1),
            false,
            false,
            false
        );
        public static Defaults Duration = new(
            2,
            "Duration",
            new Vector2(10f, 500f),
            new Vector2(40, 100),
            false,
            false,
            false
        );
        public static Defaults Density = new(
            3,
            "Density",
            new Vector2(0.1f, 10f),
            new Vector2(3, 2),
            false,
            false,
            false
        );
        public static Defaults Transpose = new(
            4,
            "Transpose",
            new Vector2(-3f, 3f),
            Vector2.zero,
            false,
            false,
            false
        );
        public static Defaults Length = new(
            5,
            "Length",
            new Vector2(10f, 1000f),
            new Vector2(200, 200),
            true,
            true,
            true
        );
    }
}