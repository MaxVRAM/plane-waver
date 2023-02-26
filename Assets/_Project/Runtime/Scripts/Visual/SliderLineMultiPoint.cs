using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using MaxVRAM.Extensions;
using UnityEngine.Serialization;

namespace PlaneWaver
{
    public class SliderLineMultiPoint : MonoBehaviour
    {
        [Header("Anchors")]
        public Transform StartAnchorTransform;
        private GameObject _startAnchor;
        public Transform EndAnchorTransform;
        private GameObject _endAnchor;
        public GameObject AnchorPrefab;
        private bool _anchorsInitialised = false;

        [Header("Nodes")]
        public GameObject NodePrefab;
        public int NodeCount = 5;
        public float NodeRadius = 0.1f;
        public List<Transform> Nodes = new();
        private bool _nodesInitialised = false;

        [Header("Joint Configs")]
        public BaseJointScriptable AnchorJoint = null;
        public BaseJointScriptable NodeNeighbourJoint = null;

        [Header("Visualisation")]
        public bool VisualiseLine = true;
        private bool _lineInitialised = false;
        private LineRenderer _line;

        void Update()
        {
            if (!Initialised())
                return;
        }

        private bool Initialised()
        {

            Nodes.RemoveAll(item => item == null);

            return true;
        }

        public void PrepareRigidbodies()
        {

        }

        private bool InitialiseAnchors()
        {
            if (AnchorPrefab == null || StartAnchorTransform == null || EndAnchorTransform == null)
                return _anchorsInitialised = false;

            if (_startAnchor == null)
                _startAnchor = Instantiate(AnchorPrefab).SetParentAndZero("Anchor.Start", StartAnchorTransform);

            if (_endAnchor == null)
                _endAnchor = Instantiate(AnchorPrefab).SetParentAndZero("Anchor.End", EndAnchorTransform);

            return _anchorsInitialised = _startAnchor != null && _endAnchor != null;
        }

        private bool InitialiseNodes()
        {
            return true;
        }

        private bool InitialiseLine()
        {
            if (!VisualiseLine)
            {
                if (_line != null)
                    _line.enabled = false;
                _lineInitialised = false;
                return true;
            }

            if (!_anchorsInitialised || !_nodesInitialised)
                return _lineInitialised = false;

            if (TryGetComponent(out _line))
                _line = gameObject.AddComponent<LineRenderer>();

            if (Nodes == null)
            {
                _nodesInitialised = false;
                return _lineInitialised = false;
            }

            _line.positionCount = Nodes.Count;

            for (int i = 0; i < Nodes.Count; i++)
            {
                _line.SetPosition(i, Nodes[i].position);
            }

            _line.enabled = VisualiseLine;
            return _lineInitialised = true;
        }
    }
}
