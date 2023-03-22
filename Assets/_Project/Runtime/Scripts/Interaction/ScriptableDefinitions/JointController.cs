using System.Collections;
using System.Collections.Generic;

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
        public JointBaseObject JointSetup;
        public bool VisualiseJointLine = true;
        private AttachmentLine _jointLine;

        private void Update()
        {
            if (!_initialised)
                return;

            UpdateLine();
        }

        public void Initialise(JointBaseObject jointConfig, Transform remoteTransform)
        {
            JointSetup = jointConfig;
            LocalTransform = transform;
            RemoteTransform = remoteTransform;
            if (!RemoteTransform.TryGetComponent(out _remoteRigidbody))
            {
                var rb = RemoteTransform.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            switch (JointSetup.GetJointType)
            {
                case JointType.Empty:
                    Debug.LogError("Joint type is empty");
                    _initialised = false;
                    return;
                case JointType.Hinge:
                    if (!TryGetComponent(out JointComponent))
                        JointComponent = gameObject.AddComponent<HingeJoint>();
                    break;
                case JointType.Fixed:
                    if (!TryGetComponent(out JointComponent))
                        JointComponent = gameObject.AddComponent<FixedJoint>();
                    break;
                case JointType.Spring:
                    if (!TryGetComponent(out JointComponent))
                        JointComponent = gameObject.AddComponent<SpringJoint>();
                    break;
                case JointType.Character:
                    if (!TryGetComponent(out JointComponent))
                        JointComponent = gameObject.AddComponent<CharacterJoint>();
                    break;
                case JointType.Configurable:
                    if (!TryGetComponent(out JointComponent))
                        JointComponent = gameObject.AddComponent<ConfigurableJoint>();
                    break;
            }

            JointComponent = JointSetup.AssignJointConfig(JointComponent);
            JointComponent.connectedBody = _remoteRigidbody;
            
            if (_jointLine == null)
                _jointLine = new GameObject("JointLine").SetParentAndZero(gameObject).AddComponent<AttachmentLine>();
            
            _jointLine.Active = false;
            _jointLine.TransformA = LocalTransform;
            _initialised = true;
        }

        private void UpdateLine()
        {
            if (_jointLine == null || JointSetup == null || LocalTransform == null || RemoteTransform == null)
                return;
            
            _jointLine.JointLineWidth = JointSetup.LineWidth;
            _jointLine.TransformB = RemoteTransform;
            _jointLine.Active = VisualiseJointLine;
        }
    }
}
