using System;
using UnityEngine;

using PlaneWaver.Modulation;

namespace PlaneWaver.Interaction
{
    public partial class Actor
    {
        #region ACTOR VALUE METHODS
        
        /// <summary>
        /// Returns the selected interaction property's latest value from this local Actor.
        /// </summary>
        /// <param name="returnValue">ref float to be updated with the latest interaction value.</param>
        /// <param name="previousVector">ref Vector3 to store an arbitrary vector for subsequent calculations.</param>
        /// <param name="selection">Actor Source Value selection enum.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public float GetActorValue(
            ref float returnValue,
            ref Vector3 previousVector,
            ModulationSourceActor selection)
        {
            switch (selection)
            {
                case ModulationSourceActor.Speed:
                    returnValue = Speed;
                    break;
                case ModulationSourceActor.Scale:
                    returnValue = Scale;
                    break;
                case ModulationSourceActor.Mass:
                    returnValue = Mass;
                    break;
                case ModulationSourceActor.MassTimesScale:
                    returnValue = Mass * Scale;
                    break;
                case ModulationSourceActor.SlideMomentum:
                    returnValue = SlideMomentum;
                    break;
                case ModulationSourceActor.AngularSpeed:
                    returnValue = AngularSpeed;
                    break;
                case ModulationSourceActor.RollMomentum:
                    returnValue = RollMomentum;
                    break;
                case ModulationSourceActor.Acceleration:
                    returnValue = Acceleration(previousVector);
                    previousVector = Velocity;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(selection), selection, null);
            }
            return returnValue;
        }

        /// <summary>
        /// Returns the selected interaction property's latest value between this local Actor and another transform.
        /// </summary>
        /// <param name="returnValue">ref float to be updated with the latest interaction value.</param>
        /// <param name="previousVector">ref Vector3 to store an arbitrary vector for subsequent calculations.</param>
        /// <param name="selection">ActorOther Source Value selection enum.</param>
        /// <param name="otherBody">Another transform to calculate relative interaction properties.</param>
        /// <returns>Float: Represents the most current value from the selected interaction parameter.</returns>
        public float GetRelativeValue(
            ref float returnValue,
            ref Vector3 previousVector,
            ModulationSourceRelational selection,
            Transform otherBody)
        {
            switch (selection)
            {
                case ModulationSourceRelational.DistanceX:
                    returnValue = Mathf.Abs(RelativePosition(otherBody).x);
                    break;
                case ModulationSourceRelational.DistanceY:
                    returnValue = Mathf.Abs(RelativePosition(otherBody).y);
                    break;
                case ModulationSourceRelational.DistanceZ:
                    returnValue = Mathf.Abs(RelativePosition(otherBody).z);
                    break;
                case ModulationSourceRelational.Radius:
                    returnValue = SphericalCoords(otherBody).Radius;
                    break;
                case ModulationSourceRelational.Polar:
                    returnValue = SphericalCoords(otherBody).Polar;
                    break;
                case ModulationSourceRelational.Elevation:
                    returnValue = SphericalCoords(otherBody).Elevation;
                    break;
                case ModulationSourceRelational.RelativeSpeed:
                    returnValue = RelativeSpeed(otherBody);
                    break;
                case ModulationSourceRelational.TangentialSpeed:
                    returnValue = TangentalSpeed(otherBody, previousVector);
                    previousVector = DirectionFromOther(otherBody);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(selection), selection, null);
            }
            return returnValue;
        }
        
        public float GetRelativeValue(
            ref float returnValue,
            ref Vector3 previousVector,
            ModulationSourceRelational selection)
        {
            if (OtherBody == null)
                return returnValue;

            GetRelativeValue(ref returnValue, ref previousVector, selection, OtherBody);
            return returnValue;
        }

        public float GetCollisionValue(ref float returnValue, ModulationSourceCollision selection)
        {
            returnValue = selection switch
            {
                ModulationSourceCollision.CollisionSpeed => CollisionSpeed,
                ModulationSourceCollision.CollisionForce => CollisionForce,
                _                                    => returnValue
            };
            return returnValue;
        }
        
        #endregion
    }
}