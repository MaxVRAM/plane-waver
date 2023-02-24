using UnityEngine;

namespace PlaneWaver
{
    public class AttachmentLine : MonoBehaviour
    {
        public bool _Active = false;
        public LineRenderer _Line;
        public Transform _TransformA;
        public Transform _TransformB;
        public Material _CustomMaterial;
        public bool _UseJointConnectionBody = false;
        private bool _LineReady = false;
        private bool _PointsReady = false;
        private float _JointLineWidth = float.MaxValue;
        public float JointLineWidth {
            get => _JointLineWidth != float.MaxValue ? _JointLineWidth : GrainBrain.Instance._AttachmentLineWidth;
            set => _JointLineWidth = value;
        }

        public bool LineInitialised()
        {
            if (_LineReady)
                return true;

            if (!TryGetComponent(out _Line))
                _Line = gameObject.AddComponent<LineRenderer>();

            _Line.material = _CustomMaterial != null ? _CustomMaterial : GrainBrain.Instance._AttachmentLineMat;

            if (_CustomMaterial != null)
                _Line.material = _CustomMaterial;
            else if (GrainBrain.Instance != null)
                _Line.material = GrainBrain.Instance._AttachmentLineMat;

            _Line.widthMultiplier = JointLineWidth;
            _Line.positionCount = 2;

            return _LineReady = true;
        }

        public bool PointsInitialised()
        {
            if (_PointsReady)
                return true;

            if (!LineInitialised())
            {
                _Line.enabled = false;
                return _PointsReady = false;
            }

            if (_TransformA == null)
                _TransformA = transform;

            _Line.SetPosition(0, _TransformA.position);

            if (_TransformB != null)
            {
                _Line.SetPosition(1, _TransformB.position);
                return _PointsReady = true;
            }

            if (!_UseJointConnectionBody || !_TransformA.TryGetComponent(out Joint joint))
            {
                _Line.enabled = false;
                return _PointsReady = false;
            }

            if (joint.connectedBody == null)
            {
                _Line.enabled = false;
                return _PointsReady = false;
            }

            _TransformB = joint.connectedBody.transform;
            _Line.SetPosition(1, _TransformB.position);
            return _PointsReady = true;
        }

        void Update()
        {
            if (!PointsInitialised())
                return;

            if (!_Active || _TransformA == null || _TransformB == null)
            {
                _Line.enabled = false;
                return;
            }

            if (Vector3.Distance(_TransformA.position, _TransformB.position) > .01f)
            {
                _Line.enabled = true;
                _Line.SetPosition(0, _TransformA.position);
                _Line.SetPosition(1, _TransformB.position);
            }
            else
            {
                _Line.enabled = false;
            }
        }
    }
}