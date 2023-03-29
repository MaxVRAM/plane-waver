using System;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class ModulationInput
    {
        public bool Enabled;
        public InputSource Source;
        public Vector2 Range;
        public bool Absolute;
        public float Factor;
        public bool Accumulate;
        public float Exponent;
        public float Smoothing;

        public ModulationInput()
        {
            Enabled = false;
            Source = new InputSource();
            Range = new Vector2(0, 1);
            Absolute = false;
            Factor = 1;
            Accumulate = false;
            Exponent = 1;
            Smoothing = 0;
        }
    }
    
    [Serializable]
    public class ModulationOutput
    {
        public ModulationLimiter Limiter;
        public float Amount;
        public bool Start;
        public bool End;

        public ModulationOutput()
        {
            Limiter = ModulationLimiter.Clip;
            Amount = 0;
            Start = false;
            End = false;
        }
    }
        
    [Serializable]
    public class ModulationNoise
    {
        public bool Enabled;
        public float Amount;
        public float Factor;
        public float PerlinSpeed;
        public bool UsePerlin;
        public bool VolatileLock;

        public ModulationNoise()
        {
            Enabled = false;
            Amount = 0;
            Factor = 0.1f;
            PerlinSpeed = 1;
            UsePerlin = false;
            VolatileLock = false;
        }
    }
}