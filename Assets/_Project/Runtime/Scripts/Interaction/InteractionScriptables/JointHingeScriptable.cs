using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver
{
    [CreateAssetMenu(fileName = "New JointHinge", menuName = "Plane Waver/Interaction/JointHinge", order = 1)]
    public class JointHingeScriptable : BaseJointScriptable
    {

        public override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointType => JointType.Hinge;
    }
}