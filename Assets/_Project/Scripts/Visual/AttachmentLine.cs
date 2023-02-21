using UnityEngine;

namespace PlaneWaver
{
    public class AttachmentLine : MonoBehaviour
    {
        public bool _Active = false;
        public LineRenderer _Line;
        public Transform _TransformA;
        public Transform _TransformB;

        void Start()
        {
            if (!TryGetComponent(out _Line))
                _Line = gameObject.AddComponent<LineRenderer>();
            
            _Line.material = GrainSynth.Instance._AttachmentLineMat;
            _Line.widthMultiplier = GrainSynth.Instance._AttachmentLineWidth;
            _Line.positionCount = 2;

            if (_TransformA != null)
                _Line.SetPosition(0, _TransformA.position);
            if (_TransformB != null)
                _Line.SetPosition(1, _TransformB.position);
            
            _Line.enabled = false;
        }

    void Update()
        {
            if (_Active && _TransformA != null && _TransformB != null)
                if (Vector3.SqrMagnitude(_TransformA.position - _TransformB.position) > .1f)
                {
                    _Line.enabled = true;
                    _Line.SetPosition(0, _TransformA.position);
                    _Line.SetPosition(1, _TransformB.position);
                }
                else
                    _Line.enabled = false;
            else
                _Line.enabled = false;
        }
    }
}
