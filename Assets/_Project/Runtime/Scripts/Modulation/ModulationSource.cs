using System;
using PlaneWaver.Interaction;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    public partial class Modulation
    {
        [Serializable]
        public class SourceSelector
        {
            public SourceGroups Group;
            public SourceMisc Misc;
            public SourceActor Actor;
            public SourceRelational Relative;
            public SourceCollision Collision;
            
            public SourceSelector() { ResetSource(); }
            
            public void ResetSource()
            {
                Group = SourceGroups.Misc;
                Misc = SourceMisc.Disabled;
                Actor = SourceActor.Speed;
                Relative = SourceRelational.Radius;
                Collision = SourceCollision.CollisionForce;
            }
        }
        
        public class InputGetter
        {
            private Vector3 _previousVector;
            private readonly Actor _actor;
            private float _value;

            public InputGetter(Actor actor)
            {
                _actor = actor;
            }

            public float GetInputValue(SourceSelector selector)
            {
                _value = selector.Group switch
                {
                    SourceGroups.Misc      => GetMiscValue(selector.Misc),
                    SourceGroups.Actor     => _actor.GetActorValue(selector.Actor, ref _previousVector),
                    SourceGroups.Relative  => _actor.GetRelativeValue(selector.Relative, ref _previousVector),
                    SourceGroups.Collision => _actor.GetCollisionValue(selector.Collision),
                    _                      => throw new ArgumentOutOfRangeException()
                };

                return _value;
            }

            public float GetMiscValue(SourceMisc misc)
            {
                return misc switch
                {
                    SourceMisc.Disabled       => _value,
                    SourceMisc.TimeSinceStart => Time.time,
                    SourceMisc.DeltaTime      => Time.deltaTime,
                    SourceMisc.SpawnAge       => _actor.Life.NormalisedAge(),
                    SourceMisc.SpawnAgeNorm   => _actor.Life.NormalisedAge(),
                    _                         => throw new ArgumentOutOfRangeException()
                };
            }
        }
    }

    public enum SourceGroups
    {
        Misc, Actor, Relative, Collision
    }

    public enum SourceMisc
    {
        Disabled, TimeSinceStart, DeltaTime, SpawnAge, SpawnAgeNorm
    }

    public enum SourceActor
    {
        Speed,
        Scale,
        Mass,
        MassTimesScale,
        AngularSpeed,
        Acceleration,
        SlideMomentum,
        RollMomentum
    }

    public enum SourceRelational
    {
        DistanceX,
        DistanceY,
        DistanceZ,
        Radius,
        Polar,
        Elevation,
        RelativeSpeed,
        TangentialSpeed
    }

    public enum SourceCollision
    {
        CollisionSpeed, CollisionForce
    }

    public enum ModulationLimiter
    {
        Clip, Repeat, PingPong
    }
}