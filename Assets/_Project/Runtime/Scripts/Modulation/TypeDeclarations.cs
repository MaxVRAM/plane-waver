
using System;

using UnityEngine;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class SourceSelection
    {
        public ModulationSourceGroups SourceGroup;
        public ModulationSourceMisc SourceMisc;
        public ModulationSourceActor SourceActor;
        public ModulationSourceRelational SourceRelational;
        public ModulationSourceCollision SourceCollision;
        
        public SourceSelection()
        {
            SourceGroup = ModulationSourceGroups.General;
            SourceMisc = ModulationSourceMisc.Disabled;
            SourceActor = ModulationSourceActor.Speed;
            SourceRelational = ModulationSourceRelational.Radius;
            SourceCollision = ModulationSourceCollision.CollisionForce;
        }
        
        // TODO - Implement the input source delegation for the processing module here
        // see the following snippet for an old example:

        // private void GenerateRawValue()
        // {
        //     switch (_ValueSource)
        //     {
        //         case ModulationSourceGroups.General:
        //             GenerateScenePropertyValue();
        //             break;
        //         case ModulationSourceGroups.PrimaryActor:
        //             _localActor.GetActorValue(ref _InputValue, ref _PreviousVector, ModulationSourceActor);
        //             break;
        //         case ModulationSourceGroups.LinkedActors:
        //             _localActor.GetActorOtherValue(ref _InputValue, ref _PreviousVector, _LinkedActors);
        //             break;
        //         case ModulationSourceGroups.ActorCollisions:
        //             _localActor.GetCollisionValue(ref _InputValue, ModulationSourceCollisions);
        //             break;
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        // }
        //
        // private void GenerateScenePropertyValue()
        // {
        //     switch (_SceneProperties)
        //     {
        //         case ModulationSourceMisc.Disabled:
        //             break;
        //         case ModulationSourceMisc.TimeSinceStart:
        //             _InputValue = Time.time;
        //             break;
        //         case ModulationSourceMisc.DeltaTime:
        //             _InputValue = Time.deltaTime;
        //             break;
        //         case ModulationSourceMisc.SpawnAge:
        //             break;
        //         case ModulationSourceMisc.SpawnAgeNorm:
        //             break;
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        // }
    }
    
    public enum ModulationSourceGroups
    {
        General, PrimaryActor, LinkedActors,
        ActorCollisions
    }

    public enum ModulationSourceMisc
    {
        Disabled, TimeSinceStart, DeltaTime,
        SpawnAge, SpawnAgeNorm
    }

    public enum ModulationSourceActor
    {
        Speed, Scale, Mass,
        MassTimesScale, AngularSpeed, Acceleration,
        SlideMomentum, RollMomentum
    }

    public enum ModulationSourceRelational
    {
        DistanceX, DistanceY, DistanceZ,
        Radius, Polar, Elevation,
        RelativeSpeed, TangentialSpeed
    }

    public enum ModulationSourceCollision
    {
        CollisionSpeed, CollisionForce
    }

    public enum ModulationLimiter
    {
        Clip, Repeat, PingPong
    }

    // TODO - remove with the old scripts
    public enum ModulationAccumulate
    {
        Replace, Accumulate
    }

    public struct ParamDefault
    {
        public readonly int Index;
        public readonly string Name;
        public readonly Vector2 Range;
        public readonly bool VolatileOnly;
        public readonly bool FixedStart;
        public readonly bool FixedEnd;

        public ParamDefault(int index, string name, Vector2 range,
                            bool volatileOnly, bool fixedStart, bool fixedEnd)
        {
            Index = index;
            Name = name;
            Range = range;
            VolatileOnly = volatileOnly;
            FixedStart = fixedStart;
            FixedEnd = fixedEnd;
        }

        public static ParamDefault Volume = new(
            0,
            "Volume",
            new Vector2(0f, 2f),
            false,
            false,
            true
        );
        public static ParamDefault Playhead = new(
            1,
            "Playhead",
            new Vector2(0f, 1f),
            false,
            false,
            false
        );
        public static ParamDefault Duration = new(
            2,
            "Duration",
            new Vector2(10f, 500f),
            false,
            false,
            false
        );
        public static ParamDefault Density = new(
            3,
            "Density",
            new Vector2(0.1f, 10f),
            false,
            false,
            false
        );
        public static ParamDefault Transpose = new(
            4,
            "Transpose",
            new Vector2(-3f, 3f),
            false,
            false,
            false
        );
        public static ParamDefault Length = new(
            5,
            "Length",
            new Vector2(10f, 1000f),
            true,
            true,
            true
        );
    }
}