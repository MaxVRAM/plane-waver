using UnityEngine;

using MaxVRAM.Extensions;

namespace PlaneWaver
{
    [System.Serializable]
    public class MaterialColourModulator : MonoBehaviour
    {
        public bool _Enabled = true;
        public Renderer _Renderer;
        public Color _Colour;
        public string _ColourParam = "_EmissiveColor";
        public bool _ActiveState = false;
        public float _ActiveIntensity = 10f;
        public float _InactiveIntensity = 0f;
        private float _CurrentIntensity = 0f;
        private float _TargetIntensity = 0f;
        [Range(0, 1)] public float _Smoothing = 0.5f;


        public void Start()
        {
            if (_Renderer == null) _Renderer = GetComponent<Renderer>();
        }

        public void Update()
        {
            if (_Enabled && _Renderer != null)
            {
                _CurrentIntensity = _CurrentIntensity.Smooth(_TargetIntensity, _Smoothing);
                _Renderer.material.SetColor(_ColourParam, _Colour * _CurrentIntensity);
            }
        }

        public void SetActiveState(bool active)
        {
            _ActiveState = active;
            _TargetIntensity = _ActiveState ? _ActiveIntensity : _InactiveIntensity;
        }

        public void Activate() { SetActiveState(true); }
        public void Deactivate() { SetActiveState(false); }
        public void Flash() { SetActiveState(false); _CurrentIntensity = _ActiveIntensity; }
    }
}