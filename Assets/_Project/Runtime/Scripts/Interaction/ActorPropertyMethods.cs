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
        public float GetActorValue(Parameter.InputActor selection, ref Vector3 previousVector)
        {
            return selection switch
            {
                Parameter.InputActor.Speed => Speed,
                Parameter.InputActor.Scale => Scale,
                Parameter.InputActor.Mass  => Mass,
                Parameter.InputActor.MassTimesScale  => Mass * Scale,
                Parameter.InputActor.SlideMomentum   => SlideMomentum,
                Parameter.InputActor.AngularSpeed    => AngularSpeed,
                Parameter.InputActor.RollMomentum    => RollMomentum,
                Parameter.InputActor.Acceleration    => Acceleration(ref previousVector),
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
        public float GetRelativeValue(Parameter.InputRelational selection, ref Vector3 previousVector, Transform otherBody)
        {
            return selection switch
            {
                Parameter.InputRelational.DistanceX => Mathf.Abs(RelativePosition(otherBody).x),
                Parameter.InputRelational.DistanceY => Mathf.Abs(RelativePosition(otherBody).y),
                Parameter.InputRelational.DistanceZ => Mathf.Abs(RelativePosition(otherBody).z),
                Parameter.InputRelational.Radius => SphericalCoords(otherBody).Radius,
                Parameter.InputRelational.Polar => SphericalCoords(otherBody).Polar,
                Parameter.InputRelational.Elevation => SphericalCoords(otherBody).Elevation,
                Parameter.InputRelational.RelativeSpeed => RelativeSpeed(otherBody),
                Parameter.InputRelational.TangentialSpeed => TangentalSpeed(otherBody, ref previousVector),
                _ => throw new ArgumentOutOfRangeException(nameof(selection), selection, null)
            };
        }

        /// <summary>
        /// Returns the selected interaction property's latest value between this local Actor and another transform.
        /// </summary>
        /// <param name="selection">ActorOther Source Value selection enum.</param>
        /// <param name="previousVector">ref Vector3 to store an arbitrary vector for subsequent calculations.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        public float GetRelativeValue(Parameter.InputRelational selection, ref Vector3 previousVector)
        {
            if (!_hasOtherBody)
                return 0;

            return GetRelativeValue(selection, ref previousVector, OtherBody);
        }

        public float GetCollisionValue(Parameter.InputCollision selection)
        {
            return selection switch
            {
                Parameter.InputCollision.CollisionSpeed => CollisionSpeed,
                Parameter.InputCollision.CollisionForce => CollisionForce,
                _                                       => 0
            };
        }
    }
}