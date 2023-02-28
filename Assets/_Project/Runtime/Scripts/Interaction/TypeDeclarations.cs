
using UnityEngine;

namespace PlaneWaver.Interaction
{
    #region ACTOR LIFE DATA

    public enum ActorBounds
    {
        Unrestricted, SpawnPosition, ControllerTransform, ColliderBounds
    }
    
    public struct ActorLifeData
    {
        public readonly float Lifespan;
        public readonly float BoundingRadius;
        public readonly ActorBounds BoundingAreaType;
        public readonly Collider BoundingCollider;
        public readonly Transform BoundingTransform;
        
        public ActorLifeData(
            float lifespan, 
            float boundingRadius,
            ActorBounds boundingAreaType, 
            Collider boundingCollider, 
            Transform boundingTransform)
        {
            Lifespan = lifespan;
            BoundingRadius = boundingRadius;
            BoundingAreaType = boundingAreaType;
            BoundingCollider = boundingCollider;
            BoundingTransform = boundingTransform;
        }
    }
    
    #endregion
}