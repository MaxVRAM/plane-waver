using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace PlaneWaver
{
    public class SliderLineMultiPoint : MonoBehaviour
    {
        [Header("Anchors")]
        public Transform _StartAnchorTransform;
        public Transform _EndAnchorTransform;
        public GameObject _AnchorPrefab;

        [Header("Nodes")]
        public GameObject _NodePrefab;
        public int _NodeCount = 5;
        public float _NodeRadius = 0.1f;
        public List<Transform> _SliderPoints = new();

        [Header("Joint Configs")]
        public BaseJointScriptable _AnchorJoint = null;
        public BaseJointScriptable _NodeNeighbourJoint = null;

        [Header("Visualisation")]
        public bool _VisualiseLine = true;
        private LineRenderer _Line;
        private bool _Initialised = false;

        void Update()
        {
            if (!Initialised())
                return;
        }

        private bool Initialised()
        {
            if (_Initialised)
                return true;

            if (_VisualiseLine && TryGetComponent(out _Line))
                _Line = gameObject.AddComponent<LineRenderer>();

            _SliderPoints.RemoveAll(item => item == null);
            _Line.positionCount = _SliderPoints.Count;

            if (_StartAnchorTransform == null)
            {
                //GameObject startAnchor = Instantiate(_AnchorPrefab);
                _StartAnchorTransform = Instantiate(_AnchorPrefab, transform).transform;
                _StartAnchorTransform.name = "Anchor.Start";
                _StartAnchorTransform.position = transform.position;
            }

            for (int i = 0; i < _SliderPoints.Count; i++)
            {
                _Line.SetPosition(i, _SliderPoints[i].position);
            }

            _Line.SetPositions(_SliderPoints.Select(t => t.position).ToArray());
            _Line.enabled = _VisualiseLine;
            return _Initialised = true;
        }

        public void PrepareRigidbodies()
        {

        }

        private void PreUpdatePointCheck()
        {

        }
    }
}
