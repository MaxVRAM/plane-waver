using System;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [Serializable]
    public struct SerializableSoftJointLimit
    {
        public float Limit;
        public float Bounciness;
        public float ContentDistance;
        
        public SerializableSoftJointLimit(SoftJointLimit softJointLimit)
        {
            Limit = softJointLimit.limit;
            Bounciness = softJointLimit.bounciness;
            ContentDistance = softJointLimit.contactDistance;
        }

        public SoftJointLimit GetOriginalStruct()
        {
            return new SoftJointLimit {
                limit = Limit,
                bounciness = Bounciness,
                contactDistance = ContentDistance
            };
        }
    }
}
