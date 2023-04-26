
using System;
using UnityEngine;

namespace PlaneWaver.Interaction
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

    public class BaseJointObject : BaseInteractionObject
    {
        public bool Enabled = true;
        public bool AutoConfigureConnectedAnchor;
        
        [Range(0f,1f)] public float JointLineWidth = 0.1f;
        public float LineWidth => JointLineWidth * 0.2f;
        
        
        protected override void Initialise()
        {
            base.Initialise();
        }

        public virtual JointType GetJointEnum => JointType.Empty;
        public virtual Type GetComponentType => typeof(Joint);

        public virtual Joint ApplyJointDataToComponent(Joint joint)
        {
            joint.autoConfigureConnectedAnchor = AutoConfigureConnectedAnchor;
            return new();
        }

        public virtual void StoreJointDataFromComponent(Joint joint)
        {
            AutoConfigureConnectedAnchor = joint.autoConfigureConnectedAnchor;
        }
        
        public static JointType ComponentToEnum(Joint joint)
        {
            return joint switch {
                HingeJoint        => JointType.Hinge,
                FixedJoint        => JointType.Fixed,
                SpringJoint       => JointType.Spring,
                CharacterJoint    => JointType.Character,
                ConfigurableJoint => JointType.Configurable,
                _                 => JointType.Empty
            };
        }
        
        public static Type ComponentToJointObjectType(Joint joint)
        {
            return joint switch {
                HingeJoint        => typeof(HingeJointObject),
                FixedJoint        => typeof(FixedJointObject),
                SpringJoint       => typeof(SpringJointObject),
                CharacterJoint    => typeof(CharacterJointObject),
                ConfigurableJoint => typeof(ConfigurableJointObject),
                _                 => typeof(BaseJointObject),
            };
        }
    }
}
