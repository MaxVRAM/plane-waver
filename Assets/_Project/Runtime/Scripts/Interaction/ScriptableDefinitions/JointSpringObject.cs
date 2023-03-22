using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [CreateAssetMenu(fileName = "Joint.Spring.", menuName = "PlaneWaver/Joint/Spring", order = 1)]
    public class JointSpringObject : JointBaseObject
    {
        public float Spring;
        public float Damper;
        public float MinDistance;
        public float MaxDistance;

        protected override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointType => JointType.Spring;

        public override Joint AssignJointConfig(Joint joint)
        {
            var springJoint = joint as SpringJoint;
            
            if (springJoint == null)
                return null;
            
            springJoint.spring = Spring;
            springJoint.damper = Damper;
            springJoint.minDistance = MinDistance;
            springJoint.maxDistance = MaxDistance;

            return springJoint;
        }
    }
}