using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using MaxVRAM.Extensions;

namespace PlaneWaver
{
    [RequireComponent(typeof(Rigidbody))]
    public class InteractionJointController : MonoBehaviour
    {
        private bool _Initialised = false;
        public Transform _LocalTransform;
        public Transform _RemoteTransform;
        private Rigidbody _RemoteRigidbody;
        public Joint _JointComponent;
        public BaseJointScriptable _JointSetup;
        public bool _VisualiseJointLine = true;
        private AttachmentLine _JointLine = null;

        void Start()
        {
        }

        void Update()
        {
            if (!_Initialised)
                return;

            UpdateLine();
        }

        public void Initialise(BaseJointScriptable jointConfig, Transform remoteTransform)
        {
            _JointSetup = jointConfig;
            _LocalTransform = transform;
            _RemoteTransform = remoteTransform;
            if (!_RemoteTransform.TryGetComponent(out _RemoteRigidbody))
            {
                Debug.LogError($"Remote transform {_RemoteTransform.name} does not have a rigidbody");
                _Initialised = false;
            }

            switch (_JointSetup.GetJointType)
            {
                case JointType.Empty:
                    Debug.LogError("Joint type is empty");
                    _Initialised = false;
                    return;
                case JointType.Hinge:
                    if (!TryGetComponent(out _JointComponent))
                        _JointComponent = gameObject.AddComponent<HingeJoint>();
                    break;
                case JointType.Fixed:
                    if (!TryGetComponent(out _JointComponent))
                        _JointComponent = gameObject.AddComponent<FixedJoint>();
                    break;
                case JointType.Spring:
                    if (!TryGetComponent(out _JointComponent))
                        _JointComponent = gameObject.AddComponent<SpringJoint>();
                    break;
                case JointType.Character:
                    if (!TryGetComponent(out _JointComponent))
                        _JointComponent = gameObject.AddComponent<CharacterJoint>();
                    break;
                case JointType.Configurable:
                    if (!TryGetComponent(out _JointComponent))
                        _JointComponent = gameObject.AddComponent<ConfigurableJoint>();
                    break;
            }

            _JointComponent = _JointSetup.AssignJointConfig(_JointComponent);
            _JointComponent.connectedBody = _RemoteRigidbody;

            _Initialised = true;
        }

        public void UpdateLine()
        {
            if (!_VisualiseJointLine)
            {
                if (_JointLine != null)
                    _JointLine._Active = false;
                return;
            }

            if (_LocalTransform == null || _RemoteTransform == null)
                return;

            if (_JointLine == null)
                _JointLine = new GameObject("JointLine").SetParentAndZero(gameObject).AddComponent<AttachmentLine>();
            
            _JointLine._TransformA = _LocalTransform;
            _JointLine._TransformB = _RemoteTransform;
            _JointLine.JointLineWidth = _JointSetup.JointLineWidth;
            _JointLine._Active = true;
        }
    }
}
