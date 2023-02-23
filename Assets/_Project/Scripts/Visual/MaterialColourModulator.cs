using UnityEngine;

using MaxVRAM.Extensions;

namespace PlaneWaver
{
    public struct MaterialColourModulator
    {
        private Renderer _Renderer;
        private Color _Colour;
        private string _ColourParam;
        private float _CurrentIntensity;
        private float _Smoothing;
        public float Smoothing { get => _Smoothing; set => _Smoothing = value; }

        public MaterialColourModulator(Renderer renderer, string colourParamName)
        {
            _Renderer = renderer;
            _Colour = renderer.material.GetColor(colourParamName);
            _ColourParam = colourParamName;
            _CurrentIntensity = 1;
            _Smoothing = 0.5f;
    }

        public MaterialColourModulator(Renderer renderer, Color colour, string colourParamName)
        {
            _Renderer = renderer;
            _Colour = colour;
            _ColourParam = colourParamName;
            _CurrentIntensity = 1;
            _Smoothing = 0.5f;
        }

        public void SetIntensity(float intensity)
        {
            if (_Renderer != null)
            {
                _CurrentIntensity = _CurrentIntensity.Smooth(intensity, _Smoothing);
                _Renderer.material.SetColor(_ColourParam, _Colour * _CurrentIntensity);
            }
        }

        public void SetColour(Color colour)
        {
            if (_Renderer != null)
                _Renderer.material.SetColor(_ColourParam, colour);
        }
    }
}