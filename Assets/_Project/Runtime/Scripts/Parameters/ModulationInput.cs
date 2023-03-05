using System;
using PlaneWaver.Interaction;
using UnityEngine;

namespace PlaneWaver.Parameters
{
    [Serializable]
    public class ModulationInputObject
    {
        public InputGroups InputGroup;
        public InputMisc MiscValue;
        public InputActor ActorValue;
        public InputRelative RelativeValue;
        public InputCollision CollisionValue;
        private Vector3 _previousVector;
        private float _previousValue;

        public ModulationInputObject()
        {
            InputGroup = InputGroups.MiscValue;
            MiscValue = InputMisc.NoInput;
            ActorValue = InputActor.Speed;
            RelativeValue = InputRelative.Radius;
            CollisionValue = InputCollision.CollisionForce;
        }

        public float GetInputValue(Actor actor)
        {
            _previousValue = InputGroup switch {
                InputGroups.MiscValue      => GetMiscValue(MiscValue, actor),
                InputGroups.ActorValue     => actor.GetActorValue(ActorValue, ref _previousVector),
                InputGroups.RelativeValue  => actor.GetRelativeValue(RelativeValue, ref _previousVector),
                InputGroups.CollisionValue => actor.GetCollisionValue(CollisionValue),
                _                     => throw new ArgumentOutOfRangeException()
            };

            return _previousValue;
        }

        public float GetMiscValue(InputMisc misc, Actor actor)
        {
            return misc switch {
                InputMisc.NoInput    => _previousValue,
                InputMisc.TimeSinceStart => Time.time,
                InputMisc.DeltaTime      => Time.deltaTime,
                InputMisc.SpawnAge       => actor.Life.NormalisedAge(),
                InputMisc.SpawnAgeNorm   => actor.Life.NormalisedAge(),
                _                        => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public enum InputGroups
    {
        MiscValue = 0, ActorValue = 1, RelativeValue = 2, CollisionValue = 3
    }

    public enum InputMisc
    {
        NoInput = 0, TimeSinceStart = 1, DeltaTime = 2, SpawnAge = 3, SpawnAgeNorm = 4
    }

    public enum InputActor
    {
        Speed = 0,
        Scale = 1,
        Mass = 2,
        MassTimesScale = 3,
        AngularSpeed = 4,
        Acceleration = 5,
        SlideMomentum = 6,
        RollMomentum = 7
    }

    public enum InputRelative
    {
        DistanceX = 0,
        DistanceY = 1,
        DistanceZ = 2,
        Radius = 3,
        Polar = 4,
        Elevation = 5,
        RelativeSpeed = 6,
        TangentialSpeed = 7
    }

    public enum InputCollision
    {
        CollisionSpeed = 0, CollisionForce = 1
    }

    public enum ModulationLimiter
    {
        Clip = 0, Repeat = 1, PingPong = 2
    }
}