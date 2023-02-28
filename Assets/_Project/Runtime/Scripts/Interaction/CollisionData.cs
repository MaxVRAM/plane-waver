
using UnityEngine;

namespace PlaneWaver.Interaction
{
    /// <summary>
    /// Ephemeral struct to hold collision data from a single collision.
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
}