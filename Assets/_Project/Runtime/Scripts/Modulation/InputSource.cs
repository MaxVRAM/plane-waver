using System;
using System.Collections.Concurrent;
using PlaneWaver.Interaction;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class InputSource
    {
        public InputGroups InputGroup;
        public InputMisc Misc;
        public InputActor Actor;
        public InputRelative Relative;
        public InputCollision Collision;
        public bool IsInstant => InputGroup == InputGroups.Collision;

        public InputSource()
        {
            InputGroup = InputGroups.Misc;
            Misc = InputMisc.Blank;
            Actor = InputActor.Speed;
            Relative = InputRelative.Radius;
            Collision = InputCollision.CollisionForce;
        }
        
        public string GetInputName()
        {
            return InputGroup switch {
                InputGroups.Misc      => Misc.ToStringCached(),
                InputGroups.Actor     => Actor.ToStringCached(),
                InputGroups.Relative  => Relative.ToStringCached(),
                InputGroups.Collision => Collision.ToStringCached(),
                _                     => throw new ArgumentOutOfRangeException()
            };
        }

        public float GetValue(ActorObject actor)
        {
            return InputGroup switch {
                InputGroups.Misc      => GetMiscValue(Misc, actor),
                InputGroups.Actor     => actor.GetActorValue(Actor),
                InputGroups.Relative  => actor.GetRelativeValue(Relative),
                InputGroups.Collision => actor.GetCollisionValue(Collision),
                _                     => throw new ArgumentOutOfRangeException()
            };
        }

        public float GetMiscValue(InputMisc misc, ActorObject actor)
        {
            return misc switch {
                InputMisc.Blank          => 0,
                InputMisc.TimeSinceStart => Time.time,
                InputMisc.DeltaTime      => Time.deltaTime,
                InputMisc.ActorAge       => actor.Controller.Age,
                InputMisc.ActorAgeNorm   => actor.Controller.NormalisedAge(),
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
        Blank = 0, TimeSinceStart = 1, DeltaTime = 2, ActorAge = 3, ActorAgeNorm = 4
    }

    public enum InputActor
    {
        Speed = 0,
        Scale = 1,
        Mass = 2,
        MassTimesScale = 3,
        Momentum = 4,
        AngularSpeed = 5,
        Acceleration = 6,
        SlideMomentum = 7,
        RollMomentum =8
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
        Clip = 0, Wrap = 1, PingPong = 2
    }
    
    public static class EnumExtensions
    {
        // https://www.meziantou.net/caching-enum-tostring-to-improve-performance.htm
        
        private static readonly ConcurrentDictionary<InputGroups, string> GroupCache = new();
        private static readonly ConcurrentDictionary<InputMisc, string> MiscCache = new();
        private static readonly ConcurrentDictionary<InputActor, string> ActorCache = new();
        private static readonly ConcurrentDictionary<InputRelative, string> RelativeCache = new();
        private static readonly ConcurrentDictionary<InputCollision, string> CollisionCache = new();

        public static string ToStringCached(this InputGroups value)
        {
            return GroupCache.GetOrAdd(value, v => v.ToString());
        }

        public static string ToStringCached(this InputMisc value)
        {
            return MiscCache.GetOrAdd(value, v => v.ToString());
        }

        public static string ToStringCached(this InputActor value)
        {
            return ActorCache.GetOrAdd(value, v => v.ToString());
        }

        public static string ToStringCached(this InputRelative value)
        {
            return RelativeCache.GetOrAdd(value, v => v.ToString());
        }

        public static string ToStringCached(this InputCollision value)
        {
            return CollisionCache.GetOrAdd(value, v => v.ToString());
        }
    }
}