using Unity.Entities;
using UnityEngine;

using MaxVRAM.Extensions;

namespace PlaneWaver
{
    /// <summary>
    /// Speakers are passed Grains entities by the GrainBrain, which they write directly to the attached AudioSource output buffer.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SpeakerAuthoring : SynthEntity
    {
        #region FIELDS & PROPERTIES

        [SerializeField] private ConnectionState _State = ConnectionState.Pooled;
        public bool IsActive => _State == ConnectionState.Active;
        [SerializeField] private int _GrainArraySize = 100;
        [SerializeField] private int _NumGrainsFree = 0;
        [SerializeField] private float _GrainLoad = 0;
        [SerializeField] private int _ConnectedHosts = 0;
        [SerializeField] private float _InactiveDuration = 0;
        [SerializeField] private float _ConnectionRadius = 1;
        [SerializeField] private float _TargetVolume = 0;

        [Range(0f,1f)]private float _VolumeSmoothing = 0.5f;
        private int _SampleRate;

        private MeshRenderer _MeshRenderer;
        private Material _Material;
        private Color _ActiveColor = new (1, 1, 1, 0.01f);
        private Color _OverloadColor = new (1, 0, 0, 0.02f);
        private Color _CurrentColour;
        private AudioSource _AudioSource;
        private Grain[] _GrainArray;

        public delegate void GrainEmitted(Grain data, int currentDSPSample);
        public event GrainEmitted OnGrainEmitted;

        #endregion

        #region ENTITY-SPECIFIC START CALL

        private void Start()
        {
            _SampleRate = AudioSettings.outputSampleRate;
            _MeshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
            if (_MeshRenderer != null) _Material = _MeshRenderer.material;
            _AudioSource = gameObject.GetComponent<AudioSource>();
            _AudioSource.rolloffMode = AudioRolloffMode.Custom;
            _AudioSource.maxDistance = 500;
            InitialiseGrainArray();

            _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _Archetype = _EntityManager.CreateArchetype(
                typeof(SpeakerComponent),
                typeof(SpeakerIndex));
        }

        #endregion

        #region SECRET SPEAKER COMPONENT BUSINESS

        public override void SetEntityType()
        {
            _EntityType = SynthEntityType.Speaker;
        }

        public override void InitialiseComponents()
        {
            _EntityManager.SetComponentData(_Entity, new SpeakerIndex { Value = EntityIndex });
            _EntityManager.SetComponentData(_Entity, new SpeakerComponent
            {
                _State = ConnectionState.Pooled,
                _ConnectionRadius = _ConnectionRadius,
                _ConnectedHostCount = 0,
                _GrainLoad = 0
            });
        }

        public override void ProcessComponents()
        {
            UpdateGrainPool();
            ProcessIndex();
            ProcessPooling();
        }

        public void ProcessIndex()
        {
            SpeakerIndex index = _EntityManager.GetComponentData<SpeakerIndex>(_Entity);
            if (EntityIndex != index.Value)
                _EntityManager.SetComponentData(_Entity, new SpeakerIndex { Value = EntityIndex });
        }

        public void ProcessPooling()
        {
            SpeakerComponent pooling = _EntityManager.GetComponentData<SpeakerComponent>(_Entity);
            _GrainLoad = _GrainLoad.Smooth(1 - (float)_NumGrainsFree / _GrainArraySize, 0.5f);
            pooling._GrainLoad = _GrainLoad;
            _EntityManager.SetComponentData(_Entity, pooling);

            bool stateChanged = _State != pooling._State;
            _State = pooling._State;
            _ConnectionRadius = pooling._ConnectionRadius;
            _ConnectedHosts = pooling._ConnectedHostCount;
            _InactiveDuration = pooling._InactiveDuration;

            transform.position = pooling._WorldPos;
            transform.localScale = Vector3.one * _ConnectionRadius;

            _TargetVolume = _State != ConnectionState.Pooled ? 1 : 0;
            _AudioSource.volume = _AudioSource.volume.Smooth(_TargetVolume, _VolumeSmoothing);

            if (_MeshRenderer == null)
                return;

            if (_State == ConnectionState.Active && !stateChanged)
            {
                _MeshRenderer.enabled = true;
                _CurrentColour = Color.Lerp(_ActiveColor, _OverloadColor, _GrainLoad);
                _Material.color = _CurrentColour;
            }
            else
            {
                _MeshRenderer.enabled = false;
                _CurrentColour = _ActiveColor;
                _Material.color = _CurrentColour;
            }
        }

        public override void Deregister()
        {
            GrainBrain.Instance.DeregisterSpeaker(this);
        }

        #endregion

        #region GRAIN POOLING MANAGEMENT

        Grain CreateNewGrain(int? numSamples = null)
        {
            int samples = numSamples.HasValue ? numSamples.Value : _SampleRate;
            return new Grain(samples);
        }

        public void SetGrainArraySize(int size)
        {
            if (size == _GrainArraySize)
                return;

            _GrainArraySize = size;
            InitialiseGrainArray();
        }

        public void InitialiseGrainArray()
        {
            _GrainArray = new Grain[_GrainArraySize];
            for (int i = 0; i < _GrainArraySize; i++)
                _GrainArray[i] = CreateNewGrain();

            _NumGrainsFree = _GrainArray.Length;
        }

        public void ResetGrainPool()
        {
            for (int i = 0; i < _GrainArray.Length; i++)
            {
                _GrainArray[i]._Pooled = true;
                _GrainArray[i]._IsPlaying = false;
            }
            _NumGrainsFree = _GrainArray.Length;
        }

        public void UpdateGrainPool()
        {
            _NumGrainsFree = 0;
            for (int i = 0; i < _GrainArray.Length; i++)
                if (!_GrainArray[i]._IsPlaying)
                {
                    _GrainArray[i]._Pooled = true;
                    _NumGrainsFree++;
                }
        }

        public void GrainAdded(Grain grainData)
        {
            if (!_EntityInitialised)
                return;
            _NumGrainsFree--;
            OnGrainEmitted?.Invoke(grainData, GrainBrain.Instance.CurrentSampleIndex);
        }

        public Grain GetEmptyGrain(out Grain grain)
        {
            grain = null;
            if (_EntityInitialised)
            {
                if (_NumGrainsFree > 0)
                    for (int i = 0; i < _GrainArray.Length; i++)
                        if (_GrainArray[i]._Pooled)
                        {
                            grain = _GrainArray[i];
                            return grain;
                        }
            }
            return grain;
        }

        #endregion

        #region AUDIO OUTPUT BUFFER POPULATION

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_EntityInitialised || _GrainArray == null || _NumGrainsFree == _GrainArraySize)
                return;

            Grain grainData;
            int _CurrentDSPSample = GrainBrain.Instance.CurrentSampleIndex;

            for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
                for (int i = 0; i < _GrainArray.Length; i++)
                {
                    if (!_GrainArray[i]._IsPlaying)
                        continue;

                    grainData = _GrainArray[i];
                    if (_CurrentDSPSample >= grainData._DSPStartTime)
                        if (grainData._PlayheadIndex >= grainData._SizeInSamples)
                        {
                            grainData._IsPlaying = false;
                        }
                        else
                        {
                            for (int chan = 0; chan < channels; chan++)
                                data[dataIndex + chan] += grainData._SampleData[grainData._PlayheadIndex];
                            grainData._PlayheadIndex++;
                        }
                }
        }

        #endregion
    }

    #region GRAIN CLASS

    public class Grain
    {
        public bool _Pooled = true;
        public bool _IsPlaying = false;
        public float[] _SampleData;
        public int _PlayheadIndex = 0;
        public float _PlayheadNormalised = 0;
        public int _SizeInSamples = -1;
        public int _DSPStartTime;

        public Grain(int maxGrainSize)
        {
            _SampleData = new float[maxGrainSize];
        }
    }

    #endregion
}
