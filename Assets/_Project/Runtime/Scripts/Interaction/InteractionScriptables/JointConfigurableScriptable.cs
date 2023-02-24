using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver
{
    [CreateAssetMenu(fileName = "New JointConfigurable", menuName = "Plane Waver/Interaction/JointConfigurable", order = 1)]
    public class JointConfigurableScriptable : BaseJointScriptable
    {
        public float _MinDistance;
        public float _MaxDistance;

        public override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointType => JointType.Configurable;

        public override Joint AssignJointConfig(Joint joint)
        {
            var configurableJoint = joint as ConfigurableJoint;
            configurableJoint.xMotion = ConfigurableJointMotion.Locked;
            configurableJoint.yMotion = ConfigurableJointMotion.Locked;
            configurableJoint.zMotion = ConfigurableJointMotion.Locked;
            configurableJoint.angularXMotion = ConfigurableJointMotion.Locked;
            configurableJoint.angularYMotion = ConfigurableJointMotion.Locked;
            configurableJoint.angularZMotion = ConfigurableJointMotion.Locked;
            configurableJoint.linearLimit = new SoftJointLimit { limit = _MaxDistance };
            configurableJoint.linearLimitSpring = new SoftJointLimitSpring { damper = 0, spring = 0 };
            configurableJoint.angularXLimitSpring = new SoftJointLimitSpring { damper = 0, spring = 0 };
            configurableJoint.angularYZLimitSpring = new SoftJointLimitSpring { damper = 0, spring = 0 };
            configurableJoint.projectionMode = JointProjectionMode.None;
            configurableJoint.projectionDistance = 0;
            configurableJoint.projectionAngle = 0;
            configurableJoint.configuredInWorldSpace = false;
            configurableJoint.swapBodies = false;
            configurableJoint.enableCollision = false;
            configurableJoint.enablePreprocessing = false;
            configurableJoint.breakForce = Mathf.Infinity;
            configurableJoint.breakTorque = Mathf.Infinity;
            configurableJoint.enablePreprocessing = false;

            return configurableJoint;
        }
    }
}