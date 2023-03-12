
using System;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    #region ACTOR LIFE DATA

    public enum ActorBounds
    {
        Unrestricted, SpawnPosition, ControllerTransform, ColliderBounds
    }
    
    public struct ActorControllerData
    {
        public readonly float Lifespan;
        public readonly float BoundingRadius;
        public readonly ActorBounds BoundingAreaType;
        public readonly Collider BoundingCollider;
        public readonly Transform BoundingTransform;
        public bool IsInitialised { get; private set; }
        
        public ActorControllerData(
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
            IsInitialised = true;
        }
        
        public static ActorControllerData Default => new ActorControllerData(
            -1, 
            30, 
            ActorBounds.SpawnPosition, 
            null, 
            null);
    }
    
    #endregion

    public enum SpawnCondition
    {
        Never, AfterSpeakersPopulated, SpeakerAvailable, AfterDelayPeriod, Always
    }

    public enum ControllerEvent
    {
        Off, OnSpawn, OnCollision, All
    };

    public enum SiblingCollision
    {
        All, Single, None
    };
}