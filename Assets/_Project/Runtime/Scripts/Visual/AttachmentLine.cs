using UnityEngine;

namespace PlaneWaver
{
    public class AttachmentLine : MonoBehaviour
    {
        public bool _Active = false;
        public LineRenderer _Line;
        public Transform _TransformA;
        public Transform _TransformB;
        public bool _SpeakerAttachment = false;
        private bool _Initialised = false;

        void Start()
        {
            if (!_Initialised)
                Initialise();
        }

        void Update()
        {
            if (!_Active || _TransformA == null || _TransformB == null ||
                (_SpeakerAttachment && !GrainBrain.Instance._DrawAttachmentLines))
            {
                _Line.enabled = false;
                return;
            }

            if (Vector3.Distance(_TransformA.position, _TransformB.position) > .1f)
            {
                _Line.enabled = true;
                _Line.SetPosition(0, _TransformA.position);
                _Line.SetPosition(1, _TransformB.position);
            }
            else
                _Line.enabled = false;
        }

        public void Initialise(Transform transformA, Transform transformB, bool speakerAttachment)
        {
            _TransformA = transformA;
            _TransformB = transformB;
            _SpeakerAttachment = speakerAttachment;
            _Active = true;
            Initialise(true);
        }

        public void Initialise(bool enableOnStart = false)
        {
            if (!TryGetComponent(out _Line))
                _Line = gameObject.AddComponent<LineRenderer>();

            _Line.material = GrainBrain.Instance._AttachmentLineMat;
            _Line.widthMultiplier = GrainBrain.Instance._AttachmentLineWidth;
            _Line.positionCount = 2;

            if (_TransformA != null)
                _Line.SetPosition(0, _TransformA.position);
            if (_TransformB != null)
                _Line.SetPosition(1, _TransformB.position);

            _Line.enabled = enableOnStart;
            _Initialised = true;
        }
    }
}