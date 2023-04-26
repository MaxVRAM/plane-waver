using MaxVRAM.Extensions;
using UnityEngine;

namespace MaxVRAM.Counters
{
    public class ValueMonitor
    {
        public bool Enabled { get; set; }
        private readonly float _smoothing;
        private readonly float _smoothingMultiplier;
        
        public float Value { get; private set; }
        public float ValuePrevious { get; private set; }
        public float ValueMin { get; private set; }
        public float ValueMax { get; private set; }
        public float DeltaPrevious { get; private set; }
        public float DeltaMax { get; private set; }
        public float DeltaAbs => Mathf.Abs(Delta);
        
        public float Delta => Value - ValuePrevious;
        public bool IsIncreasing => !Mathf.Approximately(Delta, 0f ) && Delta > 0;
        public bool IsDecreasing => !Mathf.Approximately(Delta, 0f ) && Delta < 0;

        public float SmoothedValue { get; private set; }
        public float SmoothedDelta { get; private set; }
        
        
        /// <summary>
        /// Boilerplate class for monitoring the changes in a float value; including min/max, delta, and smoothed values.
        /// </summary>
        /// <param name="enabled">If set to false, ValueMonitor will immediately return from update functions without
        /// making changes to the currently stored stats.</param>
        /// <param name="smoothing">(optional) Normalised value (0-1) defining the amount of smoothing applied to
        /// SmoothedValue and SmoothedDelta values. 0 = no smoothing applied, increasing smoothing amount towards 1.</param>
        /// <param name="smoothingMultiplier">(optional) Float to redefine the default magnitude of smoothing.</param>
        /// <param name="initialValue">(optional) What value to initialise the monitor at.</param>
        public ValueMonitor(bool enabled, float smoothing = 0.5f, float smoothingMultiplier = 10f, float initialValue = 0f)
        {
            Enabled = enabled;
            _smoothing = smoothing;
            _smoothingMultiplier = smoothingMultiplier;
            Value = initialValue;
            ValuePrevious = initialValue;
            ValueMin = initialValue;
            ValueMax = initialValue;
            DeltaMax = 0;
        }
        
        public void Reset()
        {
            Value = 0;
            ValuePrevious = 0;
            ValueMin = 0;
            ValueMax = 0;
            DeltaMax = 0;
        }

        public void Increment()
        {
            if (!Enabled) return;
            UpdatePrevious();
            Value++;
            UpdateLimitAndSmoothedValues();
        }
        
        public void Decrement()
        {
            if (!Enabled) return;
            UpdatePrevious();
            Value--;
            UpdateLimitAndSmoothedValues();
        }

        public void SetValue(float value)
        {
            if (!Enabled) return;
            UpdatePrevious();
            Value = value;
            UpdateLimitAndSmoothedValues();
        }
        
        private void UpdatePrevious()
        {
            DeltaPrevious = Delta;
            ValuePrevious = Value;
        }

        private void UpdateLimitAndSmoothedValues()
        {
            ValueMin = ValueMin < Value ? ValueMin : Value;
            ValueMax = ValueMax > Value ? ValueMax : Value;
            DeltaMax = Mathf.Abs(DeltaMax) > Mathf.Abs(Delta) ? DeltaMax : Delta;
            SmoothedValue = ValuePrevious.Smooth(Value, _smoothing);
            SmoothedDelta = DeltaPrevious.Smooth(Delta, _smoothing);
        }
    }
}