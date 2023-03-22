using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [CreateAssetMenu(fileName = "Joint.Configurable.", menuName = "PlaneWaver/Joint/Configurable", order = 1)]
    public class JointConfigurableObject : JointBaseObject
    {
        public ConfigurableJointMotion XMotion;
        public ConfigurableJointMotion YMotion;
        public ConfigurableJointMotion ZMotion;
        public ConfigurableJointMotion AngularXMotion;
        public ConfigurableJointMotion AngularYMotion;
        public ConfigurableJointMotion AngularZMotion;
        public SoftJointLimitSpring LinearLimitSpring;
        public SoftJointLimit LinearLimit;
        public SoftJointLimit AngularYLimit;
        public SoftJointLimit AngularZLimit;
        public SoftJointLimitSpring AngularXLimitSpring;
        public SoftJointLimit AngularXLimit;
        public SoftJointLimitSpring AngularYZLimitSpring;
        public SoftJointLimit AngularYZLimit;
        public Vector3 TargetPosition;
        public Vector3 TargetVelocity;
        public JointDrive XDrive;
        public JointDrive YDrive;
        public JointDrive ZDrive;
        public Quaternion TargetRotation;
        public Vector3 TargetAngularVelocity;
        public RotationDriveMode RotationDriveMode;
        public JointDrive AngularXDrive;
        public JointDrive AngularYZDrive;
        public JointDrive SlerpDrive;
        public JointProjectionMode ProjectionMode;
        public float ProjectionDistance;
        public float ProjectionAngle;
        public bool ConfiguredInWorldSpace;
        public bool SwapBodies;
        public float BreakForce;
        public float BreakTorque;
        public bool EnableCollision;
        public bool EnablePreprocessing;
        public float MassScale;
        public float ConnectedMassScale;
        
        

        protected override void Initialise()
        {
            base.Initialise();
        }

        public override JointType GetJointType => JointType.Configurable;

        public override Joint AssignJointConfig(Joint joint)
        {
            var configurableJoint = joint as ConfigurableJoint;
            
            if (configurableJoint == null)
                return null;
            
            configurableJoint.xMotion = XMotion;
            configurableJoint.yMotion = YMotion;
            configurableJoint.zMotion = ZMotion;
            configurableJoint.angularXMotion = AngularXMotion;
            configurableJoint.angularYMotion = AngularYMotion;
            configurableJoint.angularZMotion = AngularZMotion;
            configurableJoint.linearLimitSpring = LinearLimitSpring;
            configurableJoint.linearLimit = LinearLimit;
            configurableJoint.angularYLimit = AngularYLimit;
            configurableJoint.angularZLimit = AngularZLimit;
            configurableJoint.angularXLimitSpring = AngularXLimitSpring;
            configurableJoint.angularYZLimitSpring = AngularYZLimitSpring;
            configurableJoint.targetPosition = TargetPosition;
            configurableJoint.targetVelocity = TargetVelocity;
            configurableJoint.xDrive = XDrive;
            configurableJoint.yDrive = YDrive;
            configurableJoint.zDrive = ZDrive;
            configurableJoint.targetRotation = TargetRotation;
            configurableJoint.targetAngularVelocity = TargetAngularVelocity;
            configurableJoint.rotationDriveMode = RotationDriveMode;
            configurableJoint.angularXDrive = AngularXDrive;
            configurableJoint.angularYZDrive = AngularYZDrive;
            configurableJoint.slerpDrive = SlerpDrive;
            configurableJoint.projectionMode = ProjectionMode;
            configurableJoint.projectionDistance = ProjectionDistance;
            configurableJoint.projectionAngle = ProjectionAngle;
            configurableJoint.configuredInWorldSpace = ConfiguredInWorldSpace;
            configurableJoint.swapBodies = SwapBodies;
            configurableJoint.breakForce = BreakForce;
            configurableJoint.breakTorque = BreakTorque;
            configurableJoint.enableCollision = EnableCollision;
            configurableJoint.enablePreprocessing = EnablePreprocessing;
            configurableJoint.massScale = MassScale;
            configurableJoint.connectedMassScale = ConnectedMassScale;
            
            return configurableJoint;
        }
    }
}