using System;

using UnityEngine;

namespace PlaneWaver.Modulation
{
    public enum ValueLimiter
    {
        Clip, Repeat, PingPong
    }

    public enum InputOnNewValue
    {
        Replace, Accumulate
    }

    public enum InputSourceGroups
    {
        General, PrimaryActor, LinkedActors,
        ActorCollisions
    }

    public enum GeneralSources
    {
        StaticValue, TimeSinceStart, DeltaTime,
        SpawnAge, SpawnAgeNorm
    }

    public enum PrimaryActorSources
    {
        Scale, Mass, MassTimesScale,
        Speed, AngularSpeed, Acceleration,
        SlideMomentum, RollMomentum
    }

    public enum LinkedActorSources
    {
        DistanceX, DistanceY, DistanceZ,
        Radius, Polar, Elevation,
        RelativeSpeed, TangentialSpeed
    }

    public enum ActorCollisionSources
    {
        CollisionSpeed, CollisionForce
    }

    public struct ParamDefaults
    {
        public readonly int Index;
        public readonly string Name;
        public readonly Vector2 Range;
        public readonly bool VolatileOnly;
        public readonly bool FixedStart;
        public readonly bool FixedEnd;

        public ParamDefaults(int index, string name, Vector2 range,
                                 bool volatileOnly, bool fixedStart, bool fixedEnd)
        {
            Index = index;
            Name = name;
            Range = range;
            VolatileOnly = volatileOnly;
            FixedStart = fixedStart;
            FixedEnd = fixedEnd;
        }

        public static ParamDefaults Volume = new(
            0, "Volume", new Vector2(0f, 2f), false, false, true);
        public static ParamDefaults Playhead = new(
            1, "Playhead", new Vector2(0f, 1f), false, false, false);
        public static ParamDefaults Duration = new(
            2, "Duration", new Vector2(10f, 500f), false, false, false);
        public static ParamDefaults Density = new(
            3, "Density", new Vector2(0.1f, 10f), false, false, false);
        public static ParamDefaults Transpose = new(
            4, "Transpose", new Vector2(-3f, 3f), false, false, false);
        public static ParamDefaults Length = new(
            5, "Length", new Vector2(10f, 1000f), true, true, true);
    }
}