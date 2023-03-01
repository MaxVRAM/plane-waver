
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
        public bool Enabled = true;
        [Range(0f,1f)] public float JointLineWidth = 0.1f;
        public float LineWidth => JointLineWidth * 0.2f;

        public override void Initialise()
        {
            base.Initialise();
        }

        public virtual JointType GetJointType => JointType.Empty;


        public virtual Joint AssignJointConfig (Joint joint) { return new(); }
    }
}

