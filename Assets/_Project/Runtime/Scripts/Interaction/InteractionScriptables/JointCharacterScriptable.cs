using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver
{
    [CreateAssetMenu(fileName = "New JointCharacter", menuName = "Plane Waver/Interaction/JointCharacter", order = 1)]
    public class JointCharacterScriptable : BaseJointScriptable
    {

        public override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointType => JointType.Character;
    }
}