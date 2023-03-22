using UnityEngine;

namespace PlaneWaver
{
    public class AttachmentLine : MonoBehaviour
    {
        public bool Active;
        public LineRenderer Line;
        public Transform TransformA;
        public Transform TransformB;
        public Material CustomMaterial;
        public bool UseJointConnectionBody = false;
        private bool _lineReady;
        private bool _pointsReady;
        private float _jointLineWidth = -1;
        
        public float JointLineWidth {
            get => _jointLineWidth > 0 ? _jointLineWidth : SynthManager.Instance.AttachmentLineWidth;
            set => _jointLineWidth = value;
        }

        private bool LineInitialised()
        {
            if (_lineReady)
                return true;

            if (!TryGetComponent(out Line))
                Line = gameObject.AddComponent<LineRenderer>();

            Line.material = CustomMaterial != null ? CustomMaterial : SynthManager.Instance.AttachmentLineMat;

            Line.material = CustomMaterial != null ? CustomMaterial : SynthManager.Instance.AttachmentLineMat;
            Line.widthMultiplier = JointLineWidth;
            Line.positionCount = 2;

            return _lineReady = true;
        }

        private bool PointsInitialised()
        {
            if (_pointsReady)
                return true;

            if (!LineInitialised())
            {
                Line.enabled = false;
                return _pointsReady = false;
            }

            if (TransformA == null)
                TransformA = transform;

            Line.SetPosition(0, TransformA.position);

            if (TransformB != null)
            {
                Line.SetPosition(1, TransformB.position);
                return _pointsReady = true;
            }

            if (!UseJointConnectionBody || !TransformA.TryGetComponent(out Joint joint))
            {
                Line.enabled = false;
                return _pointsReady = false;
            }

            if (joint.connectedBody == null)
            {
                Line.enabled = false;
                return _pointsReady = false;
            }

            TransformB = joint.connectedBody.transform;
            Line.SetPosition(1, TransformB.position);
            return _pointsReady = true;
        }

        private void Update()
        {
            if (!PointsInitialised())
                return;

            if (!Active || TransformA == null || TransformB == null)
            {
                Line.enabled = false;
                return;
            }

            if (Vector3.Distance(TransformA.position, TransformB.position) > .01f)
            {
                Line.enabled = true;
                Line.SetPosition(0, TransformA.position);
                Line.SetPosition(1, TransformB.position);
            }
            else
            {
                Line.enabled = false;
            }
        }
    }
}