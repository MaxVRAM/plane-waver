using UnityEngine;
using Unity.Entities;

using MaxVRAM.Extensions;

namespace PlaneWaver.DSP
{
    /// <summary>
    /// Speakers are passed Grains entities by the GrainBrain, which they write directly to the attached AudioSource output buffer.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SynthSpeaker : SynthElement
    {
        #region FIELDS & PROPERTIES

        public ConnectionState State = ConnectionState.Pooled;
        public bool IsActive => State == ConnectionState.Active;
        private int _grainArraySize = 100;
        private int _numGrainsFree;
        private float _grainLoad;
        private int _connectedHosts = 0;
        private float _inactiveDuration = 0;
        private float _connectionRadius = 1;
        private float _targetVolume;
        private const float VolumeSmoothing = 0.7f;
        private int _sampleRate;
        private AudioSource _audioSource;
        private Grain[] _grainArray;

        // public delegate void GrainEmitted(Grain data, int currentDSPSample);
        // public event GrainEmitted OnGrainEmitted;

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
            EntityIndex = SynthManager.Instance.RegisterSpeaker(this);
            CreateEntity(EntityIndex);
        }

        #endregion

        #region SECRET SPEAKER COMPONENT BUSINESS

        protected override void InitialiseComponents()
        {
            Manager.SetComponentData(ElementEntity, new SpeakerIndex { Value = EntityIndex });

            Manager.SetComponentData(
                ElementEntity,
                new SpeakerComponent
                {
                    State = ConnectionState.Pooled,
                    Radius = _connectionRadius,
                    ConnectedHostCount = 0,
                    GrainLoad = 0
                }
            );
        }

        protected override void ProcessComponents()
        {
            UpdateGrainPool();
            ProcessIndex();
            ProcessAllocation();
        }

        private void ProcessIndex()
        {
            var index = Manager.GetComponentData<SpeakerIndex>(ElementEntity);

            if (EntityIndex != index.Value)
                Manager.SetComponentData(ElementEntity, new SpeakerIndex { Value = EntityIndex });
        }

        private void ProcessAllocation()
        {
            var pooling = Manager.GetComponentData<SpeakerComponent>(ElementEntity);
            _grainLoad = _grainLoad.Smooth(1 - (float)_numGrainsFree / _grainArraySize, 0.3f);
            pooling.GrainLoad = _grainLoad;
            Manager.SetComponentData(ElementEntity, pooling);

            bool stateChanged = State != pooling.State;
            State = pooling.State;
            _connectionRadius = pooling.Radius;
            _connectedHosts = pooling.ConnectedHostCount;
            _inactiveDuration = pooling.InactiveDuration;

            Transform selfTransform = transform;
            selfTransform.position = pooling.Position;
            selfTransform.localScale = Vector3.one * _connectionRadius;

            _targetVolume = State != ConnectionState.Pooled ? 1 : 0;
            _audioSource.volume = _audioSource.volume.Smooth(_targetVolume, VolumeSmoothing);
        }

        protected override void BeforeEntityDestroy() { SynthManager.Instance.DeregisterSpeaker(this); }

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

        #region GIZMOS
        
        private void OnDrawGizmos()
        {
            if (State == ConnectionState.Pooled)
                return;
            
            Color gizmoColour = IsActive ? Color.Lerp(Color.white, Color.red, _grainLoad) : Color.gray;
            gizmoColour.a = _targetVolume * (1 - SynthManager.Instance.DistanceToListener(transform));
            Gizmos.color = gizmoColour;
            Gizmos.DrawWireSphere(transform.position, _connectionRadius);
        }

        #endregion

        #region AUDIO OUTPUT BUFFER POPULATION

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!EntityInitialised || _grainArray == null || _numGrainsFree == _grainArraySize)
                return;

            //var currentDSPSample = (int)(AudioSettings.dspTime * _sampleRate);
            int currentDSPSample = SynthManager.Instance.CurrentSampleIndex;

            for (var dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
                foreach (Grain g in _grainArray)
                {
                    if (!g.IsPlaying || currentDSPSample < g.DSPStartTime)
                        continue;

                    if (g.PlayheadIndex >= g.SizeInSamples) { g.IsPlaying = false; }
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