using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using static MaxVRAM.MaxMath;

namespace PlaneWaver.Interaction
{
    public partial class Actor : MonoBehaviour
    {
        #region CLASS DEFINITIONS

        private bool _initialised = false;
        public Spawner Spawner;
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
        
        #region PHYSICS PROPERTY METHODS

        public float Acceleration(Vector3 previousVelocity) => (Velocity - previousVelocity).magnitude;
        public float Acceleration(float previousSpeed) => Speed - previousSpeed;
        public Vector3 RelativePosition(Transform other) => other.position - Position;
        public Vector3 DirectionTowardsOther(Transform other) => (other.position - Position).normalized;
        public Vector3 DirectionFromOther(Transform other) => (Position - other.position).normalized;
        public float Distance(Transform other) => Vector3.Distance(Position, other.position);

        public float DistanceFromListener() => Distance(GrainBrain.Instance.ListenerTransform);
        
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
        
        #region INITIALISATION METHODS
        
        private void InitialiseActor()
        {
            _hasSpawner = Spawner != null;
            _hasRigidbody = TryGetComponent(out _rigidbody);
            _hasCollider = TryGetComponent(out _collider);
            
            if (ActorLifeController == null)
                ActorLifeController = GetComponent<ActorLife>() ?? gameObject.AddComponent<ActorLife>();
            ActorLifeController.InitialiseActorLife(ActorLifeData);

            if (_surfaceProperties == null)
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
            return !_hasSpawner || Spawner.ContactAllowed(gameObject, other);
        }

        /// <summary>
        /// Checks if a collision is allowed based on the collision settings of the ObjectSpawner.
        /// </summary>
        /// <param name="other">The other GameObject from the collision event.</param>
        /// <returns>Boolean: True if attached emitters can trigger based on spawner config.</returns>
        private bool CollisionAllowed(GameObject other)
        {
            return !_hasSpawner || Spawner.CollisionAllowed(gameObject, other);
        }
        
        /// <summary>
        /// A shortcut reference to the OnlyTriggerMostRigid setting in the GrainBrain.
        /// </summary>
        private bool OnlyTriggerMostRigid => GrainBrain.Instance.OnlyTriggerMostRigidSurface;

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
}
