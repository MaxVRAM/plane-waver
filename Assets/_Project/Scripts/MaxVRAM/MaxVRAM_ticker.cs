using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MaxVRAM.Ticker
{
    public enum TimeUnit { seconds = 0, samples = 1 }

    public class Trigger
    {
        private TimeUnit _Unit = TimeUnit.seconds;

        private bool _CarryRemainder = true;
        private int _LastTriggerCount = 0;
        public int LastTriggerCount { get { return _LastTriggerCount; } }

        private float _TimePeriod = 1;
        private float _TimeCounter = 0;
        public float TimeCounter { get { return _TimeCounter; } }

        private int _SamplePeriod = AudioSettings.outputSampleRate;
        private int _SampleCounter = 0;
        public float SampleCounter { get { return _SampleCounter; } }

        public Trigger(TimeUnit unit, float timePeriod)
        {
            if (unit == TimeUnit.samples)
            {
                Debug.Log("ActionTimer: Sample-rate timer period can only be defined using integers. This timer will be disabled.");
                return;
            }

            _Unit = unit;
            _TimeCounter = 0;
            ChangePeriod(timePeriod);
        }

        public Trigger(TimeUnit unit, int samplePeriod)
        {
            if (unit != TimeUnit.samples)
                return;

            _Unit = unit;
            _SampleCounter = 0;
            ChangePeriod(samplePeriod);
        }

        public bool DrainTrigger()
        {
            if (_LastTriggerCount > 0)
            {
                _LastTriggerCount--;
                return true;
            }
            else
                return false;
        }

        public int UpdateTrigger(float delta, float? period)
        {
            if (_Unit != TimeUnit.seconds)
                return 0;

            if (period.HasValue)
                ChangePeriod(period.Value);

            _TimeCounter += delta;

            if (_TimeCounter < _TimePeriod)
                return 0;

            int count = (int)(_TimeCounter / _TimePeriod);
            _TimeCounter = _CarryRemainder ? _TimeCounter - _TimePeriod * count : 0;
            _LastTriggerCount = count;
            return count;
        }

        public int UpdateTrigger(int delta, int? period)
        {
            if (_Unit != TimeUnit.samples)
                return 0;

            if (period.HasValue)
                ChangePeriod(period.Value);

            _SampleCounter += delta;

            if (_SampleCounter < _SamplePeriod)
                return 0;
            else
            {
                int count = _SampleCounter / _SamplePeriod;
                _SampleCounter = _CarryRemainder ? _SampleCounter - _SamplePeriod * count : 0;
                _LastTriggerCount = count;
                return count;
            }
        }

        public void Reset(TimeUnit? unit = null)
        {
            if (unit.HasValue)
                _Unit = unit.Value;

            _TimeCounter = 0;
            _SampleCounter = 0;
        }

        public void ChangePeriod(float timePeriod)
        {
            timePeriod = Mathf.Max(timePeriod, 0.01f);
            if (timePeriod == _TimePeriod)
                return;
            _TimePeriod = timePeriod;
        }

        public void ChangePeriod(int samplePeriod)
        {
            samplePeriod = Mathf.Max(samplePeriod, 10);
            if (samplePeriod == _SamplePeriod)
                return;
            _SamplePeriod = samplePeriod;
        }

    }
}
