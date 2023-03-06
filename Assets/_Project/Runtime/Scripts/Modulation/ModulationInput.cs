using System;
using PlaneWaver.Interaction;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class ModulationInputObject
    {
        public InputGroups InputGroup;
        public InputMisc Misc;
        public InputActor Actor;
        public InputRelative Relative;
        public InputCollision Collision;
        private Vector3 _previousVector;
        private float _previousValue;

        public ModulationInputObject()
        {
            InputGroup = InputGroups.Misc;
            Misc = InputMisc.NoInput;
            Actor = InputActor.Speed;
            Relative = InputRelative.Radius;
            Collision = InputCollision.CollisionForce;
        }

        public float GetInputValue(ActorObject actor)
        {
            _previousValue = InputGroup switch {
                InputGroups.Misc      => GetMiscValue(Misc, actor),
                InputGroups.Actor     => actor.GetActorValue(Actor, ref _previousVector),
                InputGroups.Relative  => actor.GetRelativeValue(Relative, ref _previousVector),
                InputGroups.Collision => actor.GetCollisionValue(Collision),
                _                     => throw new ArgumentOutOfRangeException()
            };

            return _previousValue;
        }

        public float GetMiscValue(InputMisc misc, ActorObject actor)
        {
            return misc switch {
                InputMisc.NoInput    => _previousValue,
                InputMisc.TimeSinceStart => Time.time,
                InputMisc.DeltaTime      => Time.deltaTime,
                InputMisc.SpawnAge       => actor.Controller.NormalisedAge(),
                InputMisc.SpawnAgeNorm   => actor.Controller.NormalisedAge(),
                _                        => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public enum InputGroups
    {
        Misc = 0, Actor = 1, Relative = 2, Collision = 3
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