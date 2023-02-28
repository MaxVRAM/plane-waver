using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using PlaneWaver.Modulation;
using static MaxVRAM.MaxMath;

namespace PlaneWaver
{
    public class Actor : MonoBehaviour
    {
        #region CLASS DEFINITIONS

        private bool _initialised = false;
        public ObjectSpawner ObjectSpawner;
        private bool _hasSpawner;
        public ActorLife ActorLifeController;
        public ActorLifeData ActorLifeData;
        public Transform OtherBody;
        private List<CollisionData> _activeCollisions;
        private SurfaceProperties _surfaceProperties;
        public float SurfaceRigidity = 0.5f;
        private float _smoothedContactRigidity;
        private float _highestContactRigidity;
        private const float ContactRigiditySmoothing = 0.5f;
        private Rigidbody _rigidbody;
        private bool _hasRigidbody;
        private Collider _collider;
        private bool _hasCollider;
        private CollisionData _latestCollision;
        private bool _hasCollided;
        public Vector3 Position => transform.position;
        public Vector3 EulerAngles => transform.rotation.eulerAngles;
        public Quaternion Rotation => transform.rotation;
        public float Scale => transform.localScale.magnitude;
        public Vector3 Velocity => _hasRigidbody ? _rigidbody.velocity : Vector3.zero;
        public float Speed => _hasRigidbody ? Velocity.magnitude : 0;
        public float Mass => _hasRigidbody ? _rigidbody.mass : 0;
        public float Momentum => Speed * (Mass / 2 + 0.5f);
        public float SlideMomentum => RollMomentum != 0 ? Momentum - AngularMomentum : 0;
        public float AngularSpeed => _hasRigidbody ? _rigidbody.angularVelocity.magnitude : 0;
        public float AngularMomentum => AngularSpeed * (Mass / 2 + 0.5f);
        public float RollMomentum => IsColliding ? AngularMomentum : 0;
        public float CollisionSpeed => _hasCollided ? _latestCollision.Speed : 0;
        public float CollisionForce => _hasCollided ? _latestCollision.Force : 0;
        public bool IsColliding { get; private set; }

        #endregion

        #region INITIALISATION METHODS
        
        private void InitialiseActor()
        {
            _hasSpawner = TryGetComponent(out ObjectSpawner);
            _hasRigidbody = TryGetComponent(out _rigidbody);
            _hasCollider = TryGetComponent(out _collider);
            
            ActorLifeController = GetComponent<ActorLife>() ?? gameObject.AddComponent<ActorLife>();
            ActorLifeController.InitialiseActorLife(ActorLifeData);

            _surfaceProperties = GetComponent<SurfaceProperties>() ?? gameObject.AddComponent<SurfaceProperties>();
            SurfaceRigidity = _surfaceProperties.Rigidity;

            _activeCollisions = new ();
            _hasCollided = false;
            IsColliding = false;
            _initialised = true;
        }
        
        private void Start()
        {
            InitialiseActor();
        }
        
        #endregion

        #region UPDATE METHODS

        private void Update()
        {
            UpdateContactRigidity();
        }

        private void UpdateContactRigidity()
        {
            if (_activeCollisions.Count == 0)
            {
                _highestContactRigidity = 0;
                _smoothedContactRigidity = 0;
                return;
            }
            _highestContactRigidity = _activeCollisions.Max(x => x.Rigidity);
            _smoothedContactRigidity = Smooth(_smoothedContactRigidity,
                                              _highestContactRigidity,
                                              ContactRigiditySmoothing);
        }

        #endregion
        
        #region PHYSICS PROPERTY METHODS

        public float Acceleration(Vector3 previousVelocity) => (Velocity - previousVelocity).magnitude;
        public float Acceleration(float previousSpeed) => Speed - previousSpeed;
        public Vector3 RelativePosition(Transform other) => other.position - Position;
        public Vector3 DirectionTowardsOther(Transform other) => (other.position - Position).normalized;
        public Vector3 DirectionFromOther(Transform other) => (Position - other.position).normalized;
        public float Distance(Transform other) => Vector3.Distance(Position, other.position);

        public float RelativeSpeed(Transform other)
        {
            if (!other.TryGetComponent(out Rigidbody otherRb))
                return 0;
            
            return Vector3.Dot(otherRb.velocity, Velocity);
        }
        
        public SphericalCoordinates SphericalCoords(Transform other)
        {
            return new SphericalCoordinates(RelativePosition(other));
        }
        public Quaternion RotationDelta(Transform other, Vector3 previousDirection)
        {
            return Quaternion.FromToRotation(previousDirection, DirectionFromOther(other));
        }
        
        public float TangentalSpeed(Quaternion rotation) => TangentalSpeedFromQuaternion(rotation);
        
        public float TangentalSpeed(Transform other, Vector3 previousDirection)
        {
            return TangentalSpeedFromQuaternion(RotationDelta(other, previousDirection));
        }
        
        #endregion

        #region INTERACTION VALUES
        
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
        public float GetActorOtherValue(
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
        
        public float GetActorOtherValue(
            ref float returnValue,
            ref Vector3 previousVector,
            ModulationSourceRelational selection)
        {
            if (OtherBody == null)
                return returnValue;

            GetActorOtherValue(ref returnValue, ref previousVector, selection, OtherBody);
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
        
        #region COLLISION HANDLING

        public Action<CollisionData> OnNewValidCollision;
        
        public void OnCollisionEnter(Collision collision)
        {
            if (ContactAllowed(collision.collider.gameObject))
                _activeCollisions.Add(new CollisionData(collision));
            
            if (!CollisionAllowed(collision.collider.gameObject)) return;

            _latestCollision = new CollisionData(collision);
            _hasCollided = true;
            OnNewValidCollision?.Invoke(_latestCollision);
        }

        public void OnCollisionStay(Collision collision)
        {
            if (ContactAllowed(collision.collider.gameObject))
                IsColliding = true;
        }

        public void OnCollisionExit(Collision collision)
        {
            _activeCollisions.RemoveAll(c => c.OtherObject == collision.collider.gameObject);
            if (_activeCollisions.Count != 0) return;
            
            IsColliding = false;
            _highestContactRigidity = 0;
            _smoothedContactRigidity = 0;
        }

        /// <summary>
        /// Checks if a contact is allowed based on the contact settings of the ObjectSpawner.
        /// </summary>
        /// <param name="other">The other GameObject from the collision event.</param>
        /// <returns>Boolean: True if attached emitters should consider this collision's surface properties.</returns>
        private bool ContactAllowed(GameObject other)
        {
            return !_hasSpawner || ObjectSpawner.ContactAllowed(gameObject, other);
        }

        /// <summary>
        /// Checks if a collision is allowed based on the collision settings of the ObjectSpawner.
        /// </summary>
        /// <param name="other">The other GameObject from the collision event.</param>
        /// <returns>Boolean: True if attached emitters can trigger based on spawner config.</returns>
        private bool CollisionAllowed(GameObject other)
        {
            return !_hasSpawner || ObjectSpawner.CollisionAllowed(gameObject, other);
        }
        
        /// <summary>
        /// A shortcut reference to the OnlyTriggerMostRigid setting in the GrainBrain.
        /// </summary>
        private bool OnlyTriggerMostRigid => GrainBrain.Instance._OnlyTriggerMostRigidSurface;

        /// <summary>
        /// Checks if attached collision emitters are allowed to trigger based on collider rigidities.
        /// </summary>
        /// <param name="collisionData">CollisionData object for a specific collision.
        /// Defaults to this Actor's latest collision if no parameter provided.</param>
        /// <returns>Boolean: True if attached emitters can trigger based on rigidity.</returns>
        private bool RigidityTest(CollisionData? collisionData)
        {
            collisionData ??= _latestCollision;
            return !OnlyTriggerMostRigid || collisionData.Value.Rigidity < SurfaceRigidity;
        }
        
        #endregion
    }
    
    #region COLLISION DATA STRUCT

    /// <summary>
    /// A struct to hold collision data for a single collision.
    /// </summary>
    public struct CollisionData
    {
        public readonly float CollisionTime;
        public readonly Collision Collision;
        public readonly GameObject OtherObject;
        public readonly SurfaceProperties Surface;
        public readonly bool IsEmitter;
        public readonly bool IsMoreRigidEmitter;
        public readonly float Rigidity;
        public readonly float Speed;
        public readonly float Force;
        public readonly float Momentum;
        public readonly float Impulse;
        public readonly float Energy;

        public CollisionData(Collision collision)
        {
            CollisionTime = Time.fixedTime;
            Collision = collision;
            OtherObject = collision.collider.gameObject;
            Speed = collision.relativeVelocity.magnitude;
            Force = collision.impulse.magnitude;
            Momentum = Force * Speed;
            Impulse = Force / Time.fixedDeltaTime;
            Energy = 0.5f * Speed * Speed;

            if (OtherObject.TryGetComponent(out Surface))
            {
                Rigidity = Surface.Rigidity;
                IsEmitter = Surface.IsEmitter;
                IsMoreRigidEmitter = IsEmitter && Surface.Rigidity > Rigidity;
            }
            else
            {
                Rigidity = 1;
                IsEmitter = false;   
                IsMoreRigidEmitter = false;
            }
        }
    }

    #endregion
}
