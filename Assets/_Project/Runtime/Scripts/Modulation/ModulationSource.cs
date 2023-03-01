using System;
using PlaneWaver.Interaction;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class Source
    {
        public SourceGroups SourceGroup;
        public SourceMisc SourceMisc;
        public SourceActor SourceActor;
        public SourceRelational SourceRelative;
        public SourceCollision SourceCollision;

        private Vector3 _previousVector;
        private Actor _actor;
        public Actor Actor
        {
            get => _actor;
            set => _actor = value;
        }
        private float _value;
        public float Value
        {
            get => _value;
            set => _value = value;
        }

        public Source() { ResetSource(); }

        public Source(Actor actor) { _actor = actor; ResetSource(); }

        public void ResetSource()
        {
            SourceGroup = SourceGroups.Misc;
            SourceMisc = SourceMisc.Disabled;
            SourceActor = SourceActor.Speed;
            SourceRelative = SourceRelational.Radius;
            SourceCollision = SourceCollision.CollisionForce;
        }
        
        public float GetInputValue()
        {
            _value = SourceGroup switch
            {
                SourceGroups.Misc      => GetMiscValue(),
                SourceGroups.Actor     => _actor.GetActorValue(SourceActor, ref _previousVector),
                SourceGroups.Relative  => _actor.GetRelativeValue(SourceRelative, ref _previousVector),
                SourceGroups.Collision => _actor.GetCollisionValue(SourceCollision),
                _                      => throw new ArgumentOutOfRangeException()
            };

            return _value;
        }

        public float GetMiscValue()
        {
            return SourceMisc switch
            {
                SourceMisc.Disabled       => _value,
                SourceMisc.TimeSinceStart => Time.time,
                SourceMisc.DeltaTime      => Time.deltaTime,
                SourceMisc.SpawnAge       => _actor.ActorLifeController.NormalisedAge(),
                SourceMisc.SpawnAgeNorm   => _actor.ActorLifeController.NormalisedAge(),
                _                         => throw new ArgumentOutOfRangeException()
            };
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