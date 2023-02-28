using Unity.Entities;
using UnityEngine;

using MaxVRAM.Extensions;

namespace PlaneWaver.DSP
{
    /// <summary>
    /// Speakers are passed Grains entities by the GrainBrain, which they write directly to the attached AudioSource output buffer.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SpeakerAuthoring : SynthElement
    {
        #region FIELDS & PROPERTIES

        [SerializeField] private ConnectionState State = ConnectionState.Pooled;
        public bool IsActive => State == ConnectionState.Active;
        private int _grainArraySize = 100;
        private int _numGrainsFree = 0;
        private float _grainLoad = 0;
        private int _connectedHosts = 0;
        private float _inactiveDuration = 0;
        private float _connectionRadius = 1;
        private float _targetVolume = 0;
        private const float VolumeSmoothing = 0.5f;
        private int _sampleRate;
        private AudioSource _audioSource;
        private Grain[] _grainArray;

        public delegate void GrainEmitted(Grain data, int currentDSPSample);
        public event GrainEmitted OnGrainEmitted;

        #endregion

        #region ENTITY-SPECIFIC START CALL

        private void Start()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            _audioSource = gameObject.GetComponent<AudioSource>();
            _audioSource.rolloffMode = AudioRolloffMode.Custom;
            _audioSource.maxDistance = 500;
            InitialiseGrainArray();

            ElementType = SynthElementType.Speaker;
            Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            ElementArchetype = Manager.CreateArchetype(typeof(SpeakerComponent), typeof(SpeakerIndex));
            InitialiseEntity();
        }

        #endregion

        #region SECRET SPEAKER COMPONENT BUSINESS

        protected override void InitialiseComponents()
        {
            Manager.SetComponentData(ElementEntity, new SpeakerIndex { Value = EntityIndex });
            Manager.SetComponentData(ElementEntity, new SpeakerComponent
            {
                State = ConnectionState.Pooled,
                ConnectionRadius = _connectionRadius,
                ConnectedHostCount = 0,
                GrainLoad = 0
            });
        }

        protected override void ProcessComponents()
        {
            UpdateGrainPool();
            ProcessIndex();
            ProcessPooling();
        }

        private void ProcessIndex()
        {
            var index = Manager.GetComponentData<SpeakerIndex>(ElementEntity);
            if (EntityIndex != index.Value)
                Manager.SetComponentData(ElementEntity, new SpeakerIndex { Value = EntityIndex });
        }

        private void ProcessPooling()
        {
            var pooling = Manager.GetComponentData<SpeakerComponent>(ElementEntity);
            _grainLoad = _grainLoad.Smooth(1 - (float)_numGrainsFree / _grainArraySize, 0.5f);
            pooling.GrainLoad = _grainLoad;
            Manager.SetComponentData(ElementEntity, pooling);

            bool stateChanged = State != pooling.State;
            State = pooling.State;
            _connectionRadius = pooling.ConnectionRadius;
            _connectedHosts = pooling.ConnectedHostCount;
            _inactiveDuration = pooling.InactiveDuration;

            transform.position = pooling.WorldPos;
            transform.localScale = Vector3.one * _connectionRadius;

            _targetVolume = State != ConnectionState.Pooled ? 1 : 0;
            _audioSource.volume = _audioSource.volume.Smooth(_targetVolume, VolumeSmoothing);
        }

        protected override void Deregister()
        {
            GrainBrain.Instance.DeregisterSpeaker(this);
        }

        #endregion

        #region GRAIN POOLING MANAGEMENT

        private Grain CreateNewGrain(int? numSamples = null)
        {
            int samples = numSamples ?? _sampleRate;
            return new Grain(samples);
        }

        public void SetGrainArraySize(int size)
        {
            if (size == _grainArraySize)
                return;

            _grainArraySize = size;
            InitialiseGrainArray();
        }

        private void InitialiseGrainArray()
        {
            _grainArray = new Grain[_grainArraySize];
            for (var i = 0; i < _grainArraySize; i++)
                _grainArray[i] = CreateNewGrain();

            _numGrainsFree = _grainArray.Length;
        }

        public void ResetGrainPool()
        {
            foreach (Grain g in _grainArray)
            {
                g.Pooled = true;
                g.IsPlaying = false;
            }

            _numGrainsFree = _grainArray.Length;
        }

        private void UpdateGrainPool()
        {
            _numGrainsFree = 0;
            foreach (Grain g in _grainArray)
                if (!g.IsPlaying)
                {
                    g.Pooled = true;
                    _numGrainsFree++;
                }
        }

        public void GrainAdded(Grain grainData)
        {
            if (!EntityInitialised)
                return;
            _numGrainsFree--;
            OnGrainEmitted?.Invoke(grainData, GrainBrain.Instance.CurrentSampleIndex);
        }

        public Grain GetEmptyGrain(out Grain grain)
        {
            grain = null;
            if (!EntityInitialised || _numGrainsFree <= 0) return grain;
            
            foreach (Grain g in _grainArray)
                if (g.Pooled)
                {
                    grain = g;
                    return grain;
                }

            return grain;
        }

        #endregion

        #region AUDIO OUTPUT BUFFER POPULATION

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!EntityInitialised || _grainArray == null || _numGrainsFree == _grainArraySize)
                return;

            int currentDSPSample = GrainBrain.Instance.CurrentSampleIndex;

            for (var dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
                foreach (Grain g in _grainArray)
                {
                    if (!g.IsPlaying || currentDSPSample < g.DSPStartTime)
                        continue;
                    
                    if (g.PlayheadIndex >= g.SizeInSamples)
                    {
                        g.IsPlaying = false;
                    }
                    else
                    {
                        for (var chan = 0; chan < channels; chan++)
                            data[dataIndex + chan] += g.SampleData[g.PlayheadIndex];
                        g.PlayheadIndex++;
                    }
                }
        }

        #endregion
    }
}
