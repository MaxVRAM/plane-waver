using System;
using PlaneWaver.Interaction;
using UnityEngine;

namespace PlaneWaver.Parameters
{
    public partial class Parameter
    {
        [Serializable]
        public class ModulationInputObject
        {
            public InputGroups InputGroup;
            public InputMisc MiscInput;
            public InputActor ActorInput;
            public InputRelational RelativeInput;
            public InputCollision CollisionInput;
            private Vector3 _previousVector;
            private float _previousValue;

            public ModulationInputObject()
            {
                InputGroup = InputGroups.Misc;
                MiscInput = InputMisc.Disabled;
                ActorInput = InputActor.Speed;
                RelativeInput = InputRelational.Radius;
                CollisionInput = InputCollision.CollisionForce;
            }

            public float GetInputValue(Actor actor)
            {
                _previousValue = InputGroup switch {
                    InputGroups.Misc      => GetMiscValue(MiscInput, actor),
                    InputGroups.Actor     => actor.GetActorValue(ActorInput, ref _previousVector),
                    InputGroups.Relative  => actor.GetRelativeValue(RelativeInput, ref _previousVector),
                    InputGroups.Collision => actor.GetCollisionValue(CollisionInput),
                    _                     => throw new ArgumentOutOfRangeException()
                };

                return _previousValue;
            }

            public float GetMiscValue(InputMisc misc, Actor actor)
            {
                return misc switch {
                    InputMisc.Disabled       => _previousValue,
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
            Misc, Actor, Relative, Collision
        }

        public enum InputMisc
        {
            Disabled, TimeSinceStart, DeltaTime, SpawnAge, SpawnAgeNorm
        }

        public enum InputActor
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

        public enum InputRelational
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

        public enum InputCollision
        {
            CollisionSpeed, CollisionForce
        }

        public enum ModulationLimiter
        {
            Clip, Repeat, PingPong
        }
    }
}