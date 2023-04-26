using System;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [CreateAssetMenu(fileName = "jnt_Fixed", menuName = "PlaneWaver/Joint/Fixed", order = 1)]
    public class FixedJointObject : BaseJointObject
    {
        protected override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointEnum => JointType.Fixed;
        public override Type GetComponentType => typeof(FixedJoint);
    }
}