using System;
using UnityEngine;
using PlaneWaver.Modulation;

namespace PlaneWaver.Interaction
{
    public partial class ActorObject
    {
        /// <summary>
        /// Returns the selected interaction property's latest value from this local Actor.
        /// </summary>
        /// <param name="selection">Actor Source Value selection enum.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public float GetActorValue(InputActor selection)
        {
            return selection switch
            {
                InputActor.Speed => Speed,
                InputActor.Scale => Scale,
                InputActor.Mass  => Mass,
                InputActor.MassTimesScale  => Mass * Scale,
                InputActor.SlideMomentum   => SlideMomentum,
                InputActor.AngularSpeed    => AngularSpeed,
                InputActor.RollMomentum    => RollMomentum,
                InputActor.Acceleration    => Acceleration,
                _                          => throw new ArgumentOutOfRangeException(nameof(selection), selection, null)
            };
        }

        /// <summary>
        /// Returns the selected interaction property's latest value between this local Actor and another transform.
        /// </summary>
        /// <param name="selection">ActorOther Source Value selection enum.</param>
        /// <param name="otherBody">Another transform to calculate relative interaction properties.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        public float GetRelativeValue(InputRelative selection, Transform otherBody)
        {
            OtherBody = otherBody;
            
            return selection switch
            {
                InputRelative.DistanceX => Mathf.Abs(RelativePosition.x),
                InputRelative.DistanceY => Mathf.Abs(RelativePosition.y),
                InputRelative.DistanceZ => Mathf.Abs(RelativePosition.z),
                InputRelative.Radius => SphericalCoords.Radius,
                InputRelative.Polar => SphericalCoords.Polar,
                InputRelative.Elevation => SphericalCoords.Elevation,
                InputRelative.RelativeSpeed => RelativeSpeed(),
                InputRelative.TangentialSpeed => TangentalSpeed,
                _ => throw new ArgumentOutOfRangeException(nameof(selection), selection, null)
            };
        }

        /// <summary>
        /// Returns the selected interaction property's latest value between this local Actor and another transform.
        /// </summary>
        /// <param name="selection">ActorOther Source Value selection enum.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        public float GetRelativeValue(InputRelative selection)
        {
            if (!_hasOtherBody)
                return 0;

            return GetRelativeValue(selection, OtherBody);
        }

        public float GetCollisionValue(InputCollision selection)
        {
            return selection switch
            {
                InputCollision.CollisionSpeed => CollisionSpeed,
                InputCollision.CollisionForce => CollisionForce,
                _                                       => 0
            };
        }
    }
}