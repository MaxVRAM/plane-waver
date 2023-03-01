using System;
using UnityEngine;
using PlaneWaver.Modulation;

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
        public float GetActorValue(SourceActor selection, ref Vector3 previousVector)
        {
            float returnValue = selection switch
            {
                SourceActor.Speed          => Speed,
                SourceActor.Scale          => Scale,
                SourceActor.Mass           => Mass,
                SourceActor.MassTimesScale => Mass * Scale,
                SourceActor.SlideMomentum  => SlideMomentum,
                SourceActor.AngularSpeed   => AngularSpeed,
                SourceActor.RollMomentum   => RollMomentum,
                SourceActor.Acceleration   => Acceleration(ref previousVector),
                _                          => throw new ArgumentOutOfRangeException(nameof(selection), selection, null)
            };

            return returnValue;
        }

        /// <summary>
        /// Returns the selected interaction property's latest value between this local Actor and another transform.
        /// </summary>
        /// <param name="selection">ActorOther Source Value selection enum.</param>
        /// <param name="previousVector">ref Vector3 to store an arbitrary vector for subsequent calculations.</param>
        /// <param name="otherBody">Another transform to calculate relative interaction properties.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        public float GetRelativeValue(SourceRelational selection, ref Vector3 previousVector, Transform otherBody)
        {
            float returnValue = selection switch
            {
                SourceRelational.DistanceX => Mathf.Abs(RelativePosition(otherBody).x),
                SourceRelational.DistanceY => Mathf.Abs(RelativePosition(otherBody).y),
                SourceRelational.DistanceZ => Mathf.Abs(RelativePosition(otherBody).z),
                SourceRelational.Radius => SphericalCoords(otherBody).Radius,
                SourceRelational.Polar => SphericalCoords(otherBody).Polar,
                SourceRelational.Elevation => SphericalCoords(otherBody).Elevation,
                SourceRelational.RelativeSpeed => RelativeSpeed(otherBody),
                SourceRelational.TangentialSpeed => TangentalSpeed(otherBody, ref previousVector),
                _ => throw new ArgumentOutOfRangeException(nameof(selection), selection, null)
            };

            return returnValue;
        }

        /// <summary>
        /// Returns the selected interaction property's latest value between this local Actor and another transform.
        /// </summary>
        /// <param name="selection">ActorOther Source Value selection enum.</param>
        /// <param name="previousVector">ref Vector3 to store an arbitrary vector for subsequent calculations.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        public float GetRelativeValue(SourceRelational selection, ref Vector3 previousVector)
        {
            if (OtherBody == null)
                return 0;

            return GetRelativeValue(selection, ref previousVector, OtherBody);
        }

        public float GetCollisionValue(SourceCollision selection)
        {
            float returnValue = selection switch
            {
                SourceCollision.CollisionSpeed => CollisionSpeed,
                SourceCollision.CollisionForce => CollisionForce,
                _                              => 0
            };
            
            return returnValue;
        }
    }
}