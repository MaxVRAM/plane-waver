using System;
using UnityEngine;
using PlaneWaver.Parameters;

namespace PlaneWaver.Interaction
{
    public partial class Actor
    {
        /// <summary>
        /// Returns the selected interaction property's latest value from this local Actor.
        /// </summary>
        /// <param name="selection">Actor Source Value selection enum.</param>
        /// <param name="previousVector">ref Vector3 to store an arbitrary vector for subsequent calculations.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public float GetActorValue(InputActor selection, ref Vector3 previousVector)
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
                InputActor.Acceleration    => Acceleration(ref previousVector),
                _                          => throw new ArgumentOutOfRangeException(nameof(selection), selection, null)
            };
        }

        /// <summary>
        /// Returns the selected interaction property's latest value between this local Actor and another transform.
        /// </summary>
        /// <param name="selection">ActorOther Source Value selection enum.</param>
        /// <param name="previousVector">ref Vector3 to store an arbitrary vector for subsequent calculations.</param>
        /// <param name="otherBody">Another transform to calculate relative interaction properties.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        public float GetRelativeValue(InputRelative selection, ref Vector3 previousVector, Transform otherBody)
        {
            return selection switch
            {
                InputRelative.DistanceX => Mathf.Abs(RelativePosition(otherBody).x),
                InputRelative.DistanceY => Mathf.Abs(RelativePosition(otherBody).y),
                InputRelative.DistanceZ => Mathf.Abs(RelativePosition(otherBody).z),
                InputRelative.Radius => SphericalCoords(otherBody).Radius,
                InputRelative.Polar => SphericalCoords(otherBody).Polar,
                InputRelative.Elevation => SphericalCoords(otherBody).Elevation,
                InputRelative.RelativeSpeed => RelativeSpeed(otherBody),
                InputRelative.TangentialSpeed => TangentalSpeed(otherBody, ref previousVector),
                _ => throw new ArgumentOutOfRangeException(nameof(selection), selection, null)
            };
        }

        /// <summary>
        /// Returns the selected interaction property's latest value between this local Actor and another transform.
        /// </summary>
        /// <param name="selection">ActorOther Source Value selection enum.</param>
        /// <param name="previousVector">ref Vector3 to store an arbitrary vector for subsequent calculations.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        public float GetRelativeValue(InputRelative selection, ref Vector3 previousVector)
        {
            if (!_hasOtherBody)
                return 0;

            return GetRelativeValue(selection, ref previousVector, OtherBody);
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