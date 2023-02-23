using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver
{
    [CreateAssetMenu(fileName = "New JointFixed", menuName = "Plane Waver/Interaction/JointFixed", order = 1)]
    public class JointFixedScriptable : BaseJointScriptable
    {
        public override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointType => JointType.Fixed;
    }
}