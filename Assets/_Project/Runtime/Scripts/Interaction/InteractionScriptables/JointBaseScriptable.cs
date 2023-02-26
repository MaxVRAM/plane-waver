
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
        public bool _Enabled = true;
        [SerializeField][Range(0f,1f)] private float _JointLineWidth = 0.1f;
        public float JointLineWidth => _JointLineWidth * 0.2f;

        public override void Initialise()
        {
            base.Initialise();
        }

        public virtual JointType GetJointType => JointType.Empty;


        public virtual Joint AssignJointConfig (Joint joint) { return new(); }
    }
}

