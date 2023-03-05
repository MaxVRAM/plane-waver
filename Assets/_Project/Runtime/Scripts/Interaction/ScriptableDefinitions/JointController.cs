using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using MaxVRAM.Extensions;

namespace PlaneWaver
{
    [RequireComponent(typeof(Rigidbody))]
    public class InteractionJointController : MonoBehaviour
    {
        private bool _initialised = false;
        public Transform LocalTransform;
        public Transform RemoteTransform;
        private Rigidbody _remoteRigidbody;
        public Joint JointComponent;
        public BaseJointScriptable JointSetup;
        public bool VisualiseJointLine = true;
        private AttachmentLine _jointLine;

        void Update()
        {
            if (!_initialised)
                return;

            UpdateLine();
        }

        public void Initialise(BaseJointScriptable jointConfig, Transform remoteTransform)
        {
            JointSetup = jointConfig;
            LocalTransform = transform;
            RemoteTransform = remoteTransform;
            if (!RemoteTransform.TryGetComponent(out _remoteRigidbody))
            {
                Debug.LogError($"Remote transform {RemoteTransform.name} does not have a rigidbody");
                _initialised = false;
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

            _initialised = true;
        }

        private void UpdateLine()
        {
            if (!VisualiseJointLine)
            {
                if (_jointLine != null)
                    _jointLine._Active = false;
                return;
            }

            if (LocalTransform == null || RemoteTransform == null)
                return;

            if (_jointLine == null)
                _jointLine = new GameObject("JointLine").SetParentAndZero(gameObject).AddComponent<AttachmentLine>();
            
            _jointLine._TransformA = LocalTransform;
            _jointLine._TransformB = RemoteTransform;
            _jointLine.JointLineWidth = JointSetup.LineWidth;
            _jointLine._Active = true;
        }
    }
}
