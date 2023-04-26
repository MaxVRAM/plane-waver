using System;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [Serializable]
    public struct SerializableSoftJointLimitSpring
    {
        public float Spring;
        public float Damper;
        
        public SerializableSoftJointLimitSpring(SoftJointLimitSpring softJointLimitSpring)
        {
            Spring = softJointLimitSpring.spring;
            Damper = softJointLimitSpring.damper;
        }

        public SoftJointLimitSpring GetOriginalStruct()
        {
            return new SoftJointLimitSpring {
                spring = Spring, 
                damper = Damper
            };
        }
    }
}
