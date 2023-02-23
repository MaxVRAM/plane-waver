
using UnityEngine;

namespace PlaneWaver
{
    public enum JointType
    {
        Empty,
        Hinge,
        Fixed,
        Spring,
        Character,
        Configurable
    }

    public class BaseJointScriptable : InteractionBaseScriptable
    {
        public override void Initialise()
        {
            base.Initialise();
        }

        public virtual JointType GetJointType => JointType.Empty;

        public virtual Joint AssignJointConfig (Joint joint) { return new(); }
    }
}

