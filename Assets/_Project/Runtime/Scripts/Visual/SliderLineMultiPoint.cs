using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using MaxVRAM.Extensions;

namespace PlaneWaver
{
    public class SliderLineMultiPoint : MonoBehaviour
    {
        [Header("Anchors")]
        public Transform _StartAnchorTransform;
        private GameObject _StartAnchor;
        public Transform _EndAnchorTransform;
        private GameObject _EndAnchor;
        public GameObject _AnchorPrefab;
        private bool _AnchorsInitialised = false;

        [Header("Nodes")]
        public GameObject _NodePrefab;
        public int _NodeCount = 5;
        public float _NodeRadius = 0.1f;
        public List<Transform> _Nodes = new();
        private bool _NodesInitialised = false;

        [Header("Joint Configs")]
        public BaseJointScriptable _AnchorJoint = null;
        public BaseJointScriptable _NodeNeighbourJoint = null;

        [Header("Visualisation")]
        public bool _VisualiseLine = true;
        private bool _LineInitialised = false;
        private LineRenderer _Line;

        void Update()
        {
            if (!Initialised())
                return;
        }

        private bool Initialised()
        {

            _Nodes.RemoveAll(item => item == null);

            return true;
        }

        public void PrepareRigidbodies()
        {

        }

        private bool InitialiseAnchors()
        {
            if (_AnchorPrefab == null || _StartAnchorTransform == null || _EndAnchorTransform == null)
                return _AnchorsInitialised = false;

            if (_StartAnchor == null)
                _StartAnchor = Instantiate(_AnchorPrefab).SetParentAndZero("Anchor.Start", _StartAnchorTransform);

            if (_EndAnchor == null)
                _EndAnchor = Instantiate(_AnchorPrefab).SetParentAndZero("Anchor.End", _EndAnchorTransform);

            return _AnchorsInitialised = _StartAnchor != null && _EndAnchor != null;
        }

        private bool InitialiseNodes()
        {
            return true;
        }

        private bool InitialiseLine()
        {
            if (!_VisualiseLine)
            {
                if (_Line != null)
                    _Line.enabled = false;
                _LineInitialised = false;
                return true;
            }

            if (!_AnchorsInitialised || !_NodesInitialised)
                return _LineInitialised = false;

            if (TryGetComponent(out _Line))
                _Line = gameObject.AddComponent<LineRenderer>();

            if (_Nodes == null)
            {
                _NodesInitialised = false;
                return _LineInitialised = false;
            }

            _Line.positionCount = _Nodes.Count;

            for (int i = 0; i < _Nodes.Count; i++)
            {
                _Line.SetPosition(i, _Nodes[i].position);
            }

            _Line.enabled = _VisualiseLine;
            return _LineInitialised = true;
        }
    }
}
