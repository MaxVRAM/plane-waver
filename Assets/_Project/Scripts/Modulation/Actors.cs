using System;
using UnityEngine;

using MaxVRAM;
using static MaxVRAM.MaxMath;

namespace PlaneWaver
{
    [Serializable]
    public class Actor
    {
        private readonly Transform _Transform;
        private readonly Rigidbody _Rigidbody;
        private readonly Collider _Collider;

        private Collision _LatestCollision;
        private bool _IsColliding;
        private bool _IsAlive;

        public Actor(Transform transform)
        {
            _Transform = transform;
            _Rigidbody = transform.GetComponent<Rigidbody>();
            _Collider = transform.GetComponent<Collider>();
            _LatestCollision = null;
            _IsColliding = false;
            _IsAlive = true;
        }

        public Actor(bool isSet = false)
        {
            _Transform = null;
            _Rigidbody = null;
            _Collider = null;
            _LatestCollision = null;
            _IsColliding = false;
            _IsAlive = isSet;
        }

        public bool Exists()
        {
            _IsAlive = _Transform != null;
            return _IsAlive;
        }

        public bool HasRigidBody => _Rigidbody != null;
        public bool HasCollider => _Collider != null;
        public bool IsColliding { get => _IsColliding; set => _IsColliding = value; }

        public Transform ActorTransform => _Transform;
        public GameObject ActorGameObject => _IsAlive ? _Transform.gameObject : null;
        public Rigidbody ActorRigidbody => _Rigidbody;

        public Vector3 Position => ActorTransform.position;
        public Vector3 EulerAngles => ActorTransform.rotation.eulerAngles;
        public Quaternion Rotation => ActorTransform.rotation;
        public float Scale => ActorTransform.localScale.magnitude;

        public Vector3 Velocity => HasRigidBody ? ActorRigidbody.velocity : Vector3.zero;
        public float Speed => HasRigidBody ? Velocity.magnitude : 0;
        public float Mass => HasRigidBody ? ActorRigidbody.mass : 0;

        public float SlideMomentum => RollMomentum != 0 ? Speed * ((Mass / 2) + 0.5f) - AngularMomentum : 0; // / Mathf.Max(AngularSpeed, 1);
        public float AngularSpeed => HasRigidBody ? ActorRigidbody.angularVelocity.magnitude : 0;
        // TODO: Pulled from old code. Check why (mass / 2 + 0.5f)
        public float AngularMomentum => AngularSpeed * ((Mass / 2) + 0.5f);
        public float RollMomentum => IsColliding ? AngularMomentum : 0;
        // TODO: Refactor actor calculations to take quantum entanglement into consideration
        public Vector3 SpinVector => new(0, Rando.PickOne(new int[] { -1, 1 }), 0);
        public Collision LatestCollision { get => _LatestCollision; set => _LatestCollision = value; }
        public bool HasRegisteredCollision => _LatestCollision != null;
        public float CollisionSpeed => HasRegisteredCollision ? _LatestCollision.relativeVelocity.magnitude : 0;
        public float CollisionForce => HasRegisteredCollision ? _LatestCollision.impulse.magnitude : 0;
        public float Acceleration(Vector3 previousVelocity) { return (Velocity - previousVelocity).magnitude; }
        public float Acceleration(float previousSpeed) { return Speed - previousSpeed; }
        public Vector3 RelativePosition(Actor actorB) { return actorB.Position - Position; }
        public Vector3 DirectionAB(Actor actorB) { return (actorB.Position - Position).normalized; }
        public Vector3 DirectionBA(Actor actorB) { return (Position - actorB.Position).normalized; }
        public float Distance(Actor actorB) { return Vector3.Distance(Position, actorB.Position); }
        public float RelativeSpeed(Actor actorB) { return Vector3.Dot(actorB.ActorRigidbody.velocity, Velocity); }
        public SphericalCoords SphericalCoords(Actor actorB) { return new(RelativePosition(actorB)); }
        public Quaternion RotationDelta(Actor actorB, Vector3 previousDirection) { return Quaternion.FromToRotation(previousDirection, DirectionBA(actorB)); }
        public float TangentalSpeed(Quaternion rotation) { return TangentalSpeedFromQuaternion(rotation); }
        public float TangentalSpeed(Actor actorB, Vector3 previousDirection) { return TangentalSpeedFromQuaternion(RotationDelta(actorB, previousDirection)); }

        public float GetActorValue(ref float returnValue, ref Vector3 previousVector, PrimaryActorSources selection)
        {
            switch (selection)
            {
                case PrimaryActorSources.Speed:
                    returnValue = Speed;
                    break;
                case PrimaryActorSources.Scale:
                    returnValue = Scale;
                    break;
                case PrimaryActorSources.Mass:
                    returnValue = Mass;
                    break;
                case PrimaryActorSources.MassTimesScale:
                    returnValue = Mass * Scale;
                    break;
                case PrimaryActorSources.SlideMomentum:
                    returnValue = SlideMomentum;
                    break;
                case PrimaryActorSources.AngularSpeed:
                    returnValue = AngularSpeed;
                    break;
                case PrimaryActorSources.RollMomentum:
                    returnValue = RollMomentum;
                    break;
                case PrimaryActorSources.Acceleration:
                    returnValue = Acceleration(previousVector);
                    previousVector = Velocity;
                    break;
                default:
                    break;
            }
            return returnValue;
        }

        public float GetActorPairValue(ref float returnValue, ref Vector3 previousVector, Actor actorB, LinkedActorSources selection)
        {
            if (!actorB.Exists())
            {
                returnValue = 0;
                previousVector = Vector3.zero;
                return 0;
            }

            switch (selection)
            {
                case LinkedActorSources.DistanceX:
                    returnValue = Mathf.Abs(RelativePosition(actorB).x);
                    break;
                case LinkedActorSources.DistanceY:
                    returnValue = Mathf.Abs(RelativePosition(actorB).y);
                    break;
                case LinkedActorSources.DistanceZ:
                    returnValue = Mathf.Abs(RelativePosition(actorB).z);
                    break;
                case LinkedActorSources.Radius:
                    returnValue = SphericalCoords(actorB).Radius;
                    break;
                case LinkedActorSources.Polar:
                    returnValue = SphericalCoords(actorB).Polar;
                    break;
                case LinkedActorSources.Elevation:
                    returnValue = SphericalCoords(actorB).Elevation;
                    break;
                case LinkedActorSources.RelativeSpeed:
                    returnValue = RelativeSpeed(actorB);
                    break;
                case LinkedActorSources.TangentialSpeed:
                    returnValue = TangentalSpeed(actorB, previousVector);
                    previousVector = DirectionBA(actorB);
                    break;
                default:
                    break;
            }
            return returnValue;
        }

        public float GetCollisionValue(ref float returnValue, ActorCollisionSources selection)
        {
            switch (selection)
            {
                case ActorCollisionSources.CollisionSpeed:
                    returnValue = CollisionSpeed;
                    break;
                case ActorCollisionSources.CollisionForce:
                    returnValue = CollisionForce;
                    break;
                default:
                    break;
            }
            return returnValue;
        }
    }
}
