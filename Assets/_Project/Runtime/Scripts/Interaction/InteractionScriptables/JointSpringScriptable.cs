using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver
{
    [CreateAssetMenu(fileName = "New JointSpring", menuName = "Plane Waver/Interaction/JointSpring", order = 1)]
    public class JointSpringScriptable : BaseJointScriptable
    {
        public float _Spring;
        public float _Damper;
        public float _MinDistance;
        public float _MaxDistance;

        public override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointType => JointType.Spring;

        public override Joint AssignJointConfig(Joint joint)
        {
            var springJoint = joint as SpringJoint;
            springJoint.spring = _Spring;
            springJoint.damper = _Damper;
            springJoint.minDistance = _MinDistance;
            springJoint.maxDistance = _MaxDistance;

            return springJoint;
        }
    }
}