using System;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [Serializable]
    public struct SerializableJointDrive
    {
        public float PositionSpring;
        public float PositionDamper;
        public float MaximumForce;
        
        public SerializableJointDrive(JointDrive jointDrive)
        {
            PositionSpring = jointDrive.positionSpring;
            PositionDamper = jointDrive.positionDamper;
            MaximumForce = jointDrive.maximumForce;
        }
        
        public JointDrive GetOriginalStruct()
        {
            return new JointDrive {
                positionSpring = PositionSpring,
                positionDamper = PositionDamper,
                maximumForce = MaximumForce
            };
        }
    }
}