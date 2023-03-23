using System;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [CreateAssetMenu(fileName = "jnt_Configurable", menuName = "PlaneWaver/Joint/Configurable", order = 1)]
    public class ConfigurableJointObject : BaseJointObject
    {
        public ConfigurableJointMotion XMotion;
        public ConfigurableJointMotion YMotion;
        public ConfigurableJointMotion ZMotion;
        public ConfigurableJointMotion AngularXMotion;
        public ConfigurableJointMotion AngularYMotion;
        public ConfigurableJointMotion AngularZMotion;
        public SerializableSoftJointLimitSpring LinearLimitSpring;
        public SerializableSoftJointLimit LinearLimit;
        public SerializableSoftJointLimit AngularYLimit;
        public SerializableSoftJointLimit AngularZLimit;
        public SerializableSoftJointLimitSpring AngularXLimitSpring;
        public SerializableSoftJointLimitSpring AngularYZLimitSpring;
        public Vector3 TargetPosition;
        public Vector3 TargetVelocity;
        public SerializableJointDrive XDrive;
        public SerializableJointDrive YDrive;
        public SerializableJointDrive ZDrive;
        public Quaternion TargetRotation;
        public Vector3 TargetAngularVelocity;
        public RotationDriveMode RotationDriveMode;
        public SerializableJointDrive AngularXDrive;
        public SerializableJointDrive AngularYZDrive;
        public SerializableJointDrive SlerpDrive;
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

        public override JointType GetJointEnum => JointType.Configurable;
        public override Type GetComponentType => typeof(ConfigurableJoint);
        
        public override Joint ApplyJointDataToComponent(Joint joint)
        {
            var configurableJoint = joint as ConfigurableJoint;
            
            if (configurableJoint == null)
                return null;

            base.ApplyJointDataToComponent(joint);
            configurableJoint.xMotion = XMotion;
            configurableJoint.yMotion = YMotion;
            configurableJoint.zMotion = ZMotion;
            configurableJoint.angularXMotion = AngularXMotion;
            configurableJoint.angularYMotion = AngularYMotion;
            configurableJoint.angularZMotion = AngularZMotion;
            configurableJoint.linearLimitSpring = LinearLimitSpring.GetOriginalStruct();
            configurableJoint.linearLimit = LinearLimit.GetOriginalStruct();
            configurableJoint.angularYLimit = AngularYLimit.GetOriginalStruct();
            configurableJoint.angularZLimit = AngularZLimit.GetOriginalStruct();
            configurableJoint.angularXLimitSpring = AngularXLimitSpring.GetOriginalStruct();
            configurableJoint.angularYZLimitSpring = AngularYZLimitSpring.GetOriginalStruct();
            configurableJoint.targetPosition = TargetPosition;
            configurableJoint.targetVelocity = TargetVelocity;
            configurableJoint.xDrive = XDrive.GetOriginalStruct();
            configurableJoint.yDrive = YDrive.GetOriginalStruct();
            configurableJoint.zDrive = ZDrive.GetOriginalStruct();
            configurableJoint.targetRotation = TargetRotation;
            configurableJoint.targetAngularVelocity = TargetAngularVelocity;
            configurableJoint.rotationDriveMode = RotationDriveMode;
            configurableJoint.angularXDrive = AngularXDrive.GetOriginalStruct();
            configurableJoint.angularYZDrive = AngularYZDrive.GetOriginalStruct();
            configurableJoint.slerpDrive = SlerpDrive.GetOriginalStruct();
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

        public override void StoreJointDataFromComponent(Joint joint)
        {
            if (joint == null)
                return;
            
            var configurableJoint = (ConfigurableJoint)joint;
            base.StoreJointDataFromComponent(joint);
            XMotion = configurableJoint.xMotion;
            YMotion = configurableJoint.yMotion;
            ZMotion = configurableJoint.zMotion;
            AngularXMotion = configurableJoint.angularXMotion;
            AngularYMotion = configurableJoint.angularYMotion;
            AngularZMotion = configurableJoint.angularZMotion;
            LinearLimitSpring = new SerializableSoftJointLimitSpring(configurableJoint.linearLimitSpring);
            LinearLimit = new SerializableSoftJointLimit(configurableJoint.linearLimit);
            AngularYLimit = new SerializableSoftJointLimit(configurableJoint.angularYLimit);
            AngularZLimit = new SerializableSoftJointLimit(configurableJoint.angularZLimit);
            AngularXLimitSpring = new SerializableSoftJointLimitSpring(configurableJoint.angularXLimitSpring);
            AngularYZLimitSpring = new SerializableSoftJointLimitSpring(configurableJoint.angularYZLimitSpring);
            TargetPosition = configurableJoint.targetPosition;
            TargetVelocity = configurableJoint.targetVelocity;
            XDrive = new SerializableJointDrive(configurableJoint.xDrive);
            YDrive = new SerializableJointDrive(configurableJoint.yDrive);
            ZDrive = new SerializableJointDrive(configurableJoint.zDrive);
            TargetRotation = configurableJoint.targetRotation;
            TargetAngularVelocity = configurableJoint.targetAngularVelocity;
            RotationDriveMode = configurableJoint.rotationDriveMode;
            AngularXDrive = new SerializableJointDrive(configurableJoint.angularXDrive);
            AngularYZDrive = new SerializableJointDrive(configurableJoint.angularYZDrive);
            SlerpDrive = new SerializableJointDrive(configurableJoint.slerpDrive);
            ProjectionMode = configurableJoint.projectionMode;
            ProjectionDistance = configurableJoint.projectionDistance;
            ProjectionAngle = configurableJoint.projectionAngle;
            ConfiguredInWorldSpace = configurableJoint.configuredInWorldSpace;
            SwapBodies = configurableJoint.swapBodies;
            BreakForce = configurableJoint.breakForce;
            BreakTorque = configurableJoint.breakTorque;
            EnableCollision = configurableJoint.enableCollision;
            EnablePreprocessing = configurableJoint.enablePreprocessing;
            MassScale = configurableJoint.massScale;
            ConnectedMassScale = configurableJoint.connectedMassScale;
        }
    }
}
