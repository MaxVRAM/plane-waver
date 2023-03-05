using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver
{
    [CreateAssetMenu(fileName = "Joint.Character.", menuName = "PlaneWaver/Joint/Character", order = 1)]
    public class JointCharacterScriptable : BaseJointScriptable
    {

        public override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointType => JointType.Character;
    }
}