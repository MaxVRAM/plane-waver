using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [CreateAssetMenu(fileName = "jnt_Hinge", menuName = "PlaneWaver/Joint/Hinge", order = 1)]
    public class HingeJointObject : BaseJointObject
    {

        protected override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointEnum => JointType.Hinge;
        public override Type GetComponentType => typeof(HingeJoint);
    }
}