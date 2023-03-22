using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [CreateAssetMenu(fileName = "Joint.Hinge.", menuName = "PlaneWaver/Joint/Hinge", order = 1)]
    public class JointHingeObject : JointBaseObject
    {

        protected override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointType => JointType.Hinge;
    }
}