using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [CreateAssetMenu(fileName = "Joint.Fixed", menuName = "PlaneWaver/Joint/Fixed", order = 1)]
    public class JointFixedObject : JointBaseObject
    {
        protected override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointType => JointType.Fixed;
    }
}