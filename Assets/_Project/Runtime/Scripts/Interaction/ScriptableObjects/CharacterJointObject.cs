using System;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [CreateAssetMenu(fileName = "jnt_Character", menuName = "PlaneWaver/Joint/Character", order = 1)]
    public class CharacterJointObject : BaseJointObject
    {

        protected override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointEnum => JointType.Character;
        public override Type GetComponentType => typeof(CharacterJoint);
    }
}