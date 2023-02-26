using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver
{
    public class SliderLineMultiPoint : MonoBehaviour
    {
        public List<Transform> _SliderPoints;
        private LineRenderer _Line;
        private bool _Initialised = false;

        private List<Transform>.Enumerator GetEnumerator()
        {
            return _SliderPoints.GetEnumerator();
        }

        void Update()
        {
            if (!LineConfigured())
                return;

            _Line.SetPositions(enumerator(t => t.position).ToArray());
        }

        private bool LineConfigured()
        {
            if (_Initialised)
                return true;

            _SliderPoints.RemoveAll(item => item == null);

            for (int i = 0; i < _SliderPoints.Count; i++)
            {
                if (_SliderPoints[i] == null)
                    return _Initialised = false;

            if (TryGetComponent(out _Line))
                _Line = gameObject.AddComponent<LineRenderer>();

            if (_Line == null)
                return _Initialised = false;

            _Line.positionCount = _SliderPoints.Length;
            _Line.SetPositions(_SliderPoints.Select(t => t.position).ToArray());
            _Line.enabled = true;
            return _Initialised = true;
        }
    }
}