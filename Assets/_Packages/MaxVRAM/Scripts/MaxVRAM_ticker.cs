
using UnityEngine;


namespace MaxVRAM.Ticker
{
    public enum TimeUnit { Seconds = 0, Samples = 1 }

    public class Trigger
    {
        private TimeUnit _unit = TimeUnit.Seconds;

        private const bool CarryRemainder = true;
        private int LastTriggerCount { get; set; }

        private float _timePeriod = 1;
        private float TimeCounter { get; set; }

        private int _samplePeriod = AudioSettings.outputSampleRate;
        private int SampleCounter { get; set; }

        public Trigger(TimeUnit unit, float timePeriod)
        {
            if (unit == TimeUnit.Samples)
            {
                Debug.Log("ActionTimer: Sample-rate timer period can only be defined using integers.");
                return;
            }

            _unit = unit;
            TimeCounter = 0;
            ChangePeriod(timePeriod);
        }

        public Trigger(TimeUnit unit, int samplePeriod)
        {
            if (unit != TimeUnit.Samples)
                return;

            _unit = unit;
            SampleCounter = 0;
            ChangePeriod(samplePeriod);
        }

        public bool DrainTrigger()
        {
            if (LastTriggerCount > 0)
            {
                LastTriggerCount--;
                return true;
            }
            else
                return false;
        }

        public int UpdateTrigger(float delta, float? period)
        {
            if (_unit != TimeUnit.Seconds)
                return 0;

            if (period.HasValue)
                ChangePeriod(period.Value);

            TimeCounter += delta;

            if (TimeCounter < _timePeriod)
                return 0;

            int count = (int)(TimeCounter / _timePeriod);
            TimeCounter = CarryRemainder ? TimeCounter - _timePeriod * count : 0;
            LastTriggerCount = count;
            return count;
        }

        public int UpdateTrigger(int delta, int? period)
        {
            if (_unit != TimeUnit.Samples)
                return 0;

            if (period.HasValue)
                ChangePeriod(period.Value);

            SampleCounter += delta;

            if (SampleCounter < _samplePeriod)
                return 0;
            else
            {
                int count = SampleCounter / _samplePeriod;
                SampleCounter = CarryRemainder ? SampleCounter - _samplePeriod * count : 0;
                LastTriggerCount = count;
                return count;
            }
        }

        public void Reset(TimeUnit? unit = null)
        {
            if (unit.HasValue)
                _unit = unit.Value;

            TimeCounter = 0;
            SampleCounter = 0;
        }

        public void ChangePeriod(float timePeriod)
        {
            timePeriod = Mathf.Max(timePeriod, 0.01f);
            if (Mathf.Approximately(timePeriod, _timePeriod))
                return;
            _timePeriod = timePeriod;
        }

        public void ChangePeriod(int samplePeriod)
        {
            samplePeriod = Mathf.Max(samplePeriod, 10);
            if (samplePeriod.Equals(_samplePeriod))
                return;
            _samplePeriod = samplePeriod;
        }

    }
}
