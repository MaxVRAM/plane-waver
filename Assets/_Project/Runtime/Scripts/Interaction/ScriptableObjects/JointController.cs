using System;
using UnityEngine;
using MaxVRAM.Extensions;

namespace PlaneWaver.Interaction
{
    [RequireComponent(typeof(Rigidbody))]
    public class JointController : MonoBehaviour
    {
        private bool _initialised;
        public Transform LocalTransform;
        public Transform RemoteTransform;
        private Rigidbody _remoteRigidbody;
        public Joint JointComponent;
        public BaseJointObject JointDataObject;
        public bool VisualiseJointLine = true;
        private AttachmentLine _jointLine;

        private void Update()
        {
            if (_initialised)
                UpdateLine();
        }

        public void Initialise(BaseJointObject jointConfig, Transform remoteTransform)
        {
            JointDataObject = jointConfig;
            LocalTransform = transform;
            RemoteTransform = remoteTransform;
            
            if (!RemoteTransform.TryGetComponent(out _remoteRigidbody))
            {
                var rb = RemoteTransform.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }
            
            Type jointType = JointDataObject.GetComponentType;
            
            if (JointComponent != null)
                Destroy(JointComponent);
            
            Joint[] joints = gameObject.GetComponents<Joint>();
            
            foreach (Joint joint in joints)
                Destroy(joint);
            
            JointComponent = gameObject.AddComponent(jointType) as Joint;
            JointComponent = JointDataObject.ApplyJointDataToComponent(JointComponent);
            JointComponent.connectedBody = _remoteRigidbody;
            
            if (_jointLine == null)
                _jointLine = new GameObject("JointLine").SetParentAndZero(gameObject).AddComponent<AttachmentLine>();
            
            _jointLine.Active = false;
            _jointLine.TransformA = LocalTransform;
            _initialised = true;
        }

        private void UpdateLine()
        {
            if (_jointLine == null || JointDataObject == null || LocalTransform == null || RemoteTransform == null)
                return;
            
            _jointLine.JointLineWidth = JointDataObject.LineWidth;
            _jointLine.TransformB = RemoteTransform;
            _jointLine.Active = VisualiseJointLine;
        }
    }
}
