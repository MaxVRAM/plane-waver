using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using TMPro;

using NaughtyAttributes;

using MaxVRAM.Ticker;
using MaxVRAM.Audio;

// PROJECT AUDIO CONFIGURATION NOTES
// ---------------------------------
// DSP Buffer size in audio settings
// Best performance - 46.43991
// Good latency - 23.21995
// Best latency - 11.60998

namespace PlaneWaver
{
    /// <summary>
    /// Single-instanced manager component that governs the synthesiser's entities and grain delivery.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GrainBrain : MonoBehaviour
    {
        #region FIELDS & PROPERTIES

        public static GrainBrain Instance;

        private EntityManager _EntityManager;
        private EntityQuery _GrainQuery;

        private Entity _WindowingEntity;
        private Entity _AudioTimerEntity;
        private Entity _AttachmentEntity;

        private AudioListener _Listener;

        private int _SampleRate = 44100;
        private int _CurrentSampleIndex = 0;
        private int _FrameStartSampleIndex = 0;
        private int _LastFrameSampleDuration = 0;
        private float _GrainsPerFrame = 0;
        private float _GrainsPerSecond = 0;
        private float _GrainsPerSecondPeak = 0;
        private int _GrainsDiscarded = 0;
        private float _AverageGrainAge = 0;
        private float _AverageGrainAgeMS = 0;
        private int _SpeakerCount;
        private int _HostCount;
        private int _EmitterCount;

        [Tooltip("Maximum number of speakers possible, defined by 'numRealVoices' in the project settings audio tab.")]
        [ShowNonSerializedField] private int _MaxSpeakers = 0;

        [Tooltip("Maximum distance from the listener to enable emitters and allocate speakers.")]
        [BoxGroup("Audio Config")][Range(0.1f, 50)] public float _ListenerRadius = 30;
        [Tooltip("Additional ms to calculate and queue grains each frame. Set to 0, the grainComponent queue equals the previous frame's duration. Adds latency, but help to avoid underrun. Recommended values > 20ms.")]
        [BoxGroup("Audio Config")][Range(0, 100)] public float _QueueDurationMS = 22;
        [Tooltip("Percentage of previous frame duration to delay grainComponent start times of next frame. Adds a more predictable amount of latency to help avoid timing issues when the framerate slows.")]
        [BoxGroup("Audio Config")][Range(0, 100)] public float _DelayPercentLastFrame = 10;
        [Tooltip("Discard unplayed grains with a DSP start index more than this value (ms) in the past. Prevents clustered grainComponent playback when resources are near their limit.")]
        [BoxGroup("Audio Config")][Range(0, 60)] public float _DiscardGrainsOlderThanMS = 10;
        [Tooltip("Delay bursts triggered on the same frame by a random amount. Helps avoid phasing issues caused by identical emitters triggered together.")]
        [BoxGroup("Audio Config")][Range(0, 40)] public float _BurstStartOffsetRangeMS = 8;
        [Tooltip("Burst emitters ignore subsequent collisions for this duration to avoid fluttering from weird physics.")]
        [BoxGroup("Audio Config")][Range(0, 50)] public float _BurstDebounceDurationMS = 25;
        [BoxGroup("Audio Config")][SerializeField] private WindowFunction _GrainEnvelope;

        private int _SamplesPerMS = 0;
        public int SampleRate => _SampleRate;
        public int SamplesPerMS => _SamplesPerMS;
        public int CurrentSampleIndex => _CurrentSampleIndex;
        public int QueueDurationSamples => (int)(_QueueDurationMS * SamplesPerMS);
        public int BurstStartOffsetRange => (int)(_BurstStartOffsetRangeMS * SamplesPerMS);
        public int GrainDiscardSampleIndex => _FrameStartSampleIndex - (int)(_DiscardGrainsOlderThanMS * SamplesPerMS);
        public int NextFrameIndexEstimate => _FrameStartSampleIndex + (int)(_LastFrameSampleDuration * (1 + _DelayPercentLastFrame / 100));
        
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Target number of speakers to be spawned and managed by the synth system.")]
        [Range(0, 255)][SerializeField] private int _SpeakersAllocated = 32;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Period (seconds) to instantiate/destroy speakers. Affects performance only during start time or when altering the 'Speakers Allocated' value during runtime.")]
        [Range(0.01f, 1)][SerializeField] private float _SpeakerAllocationPeriod = 0.2f;
        private Trigger _SpeakerAllocationTimer;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Speaker prefab to spawn when dynamically allocating speakers.")]
        public SpeakerAuthoring _SpeakerPrefab;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("ActorTransform to contain spawned speakers.")]
        [SerializeField] private Transform _SpeakerParentTransform;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("World coordinates to store pooled speakers.")]
        [SerializeField] private Vector3 _SpeakerPoolingPosition = Vector3.down * 20;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Number of grains allocated to each speaker. Every frame the synth manager distributes grains to each grain's target speaker, which holds on to the grain object until all samples have been written to the output buffer.")]
        [Range(0, 255)][SerializeField] private int _SpeakerGrainArraySize = 100;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("The ratio of busy(?):(1)empty grains in each speaker before it is considered 'busy' and deprioritised as a target for additional emitters by the attachment system.")]
        [Range(0.1f, 45)] public float _SpeakerBusyLoadLimit = 0.5f;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Arc length in degrees from the listener position that emitters can be attached to a speaker.")]
        [Range(0.1f, 45)] public float _SpeakerAttachArcDegrees = 10;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("How quicklyt speakers follow their targets. Increasing this value helps the speaker track its target, but can start invoking inappropriate doppler if tracking high numbers of ephemeral emitters.")]
        [Range(0, 50)] public float _SpeakerTrackingSpeed = 20;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Length of time in milliseconds before pooling a speaker after its last emitter has disconnected. Allows speakers to be reused without destroying remaining grains from destroyed emitters.")]
        [Range(0, 500)] public float _SpeakerLingerTime = 100;
        public int SpeakersAllocated => Math.Min(_SpeakersAllocated, _MaxSpeakers);
        public bool PopulatingSpeakers => _SpeakersAllocated != _Speakers.Count;
        public float AttachSmoothing => Mathf.Clamp(Time.deltaTime * _SpeakerTrackingSpeed, 0, 1);

        [BoxGroup("Visual Feedback")]
        public TextMeshProUGUI _StatsValuesText;
        [BoxGroup("Visual Feedback")]
        public TextMeshProUGUI _StatsValuesPeakText;
        [BoxGroup("Visual Feedback")]
        public bool _DrawAttachmentLines = false;
        [BoxGroup("Visual Feedback")]
        public Material _AttachmentLineMat;
        [BoxGroup("Visual Feedback")]
        [Range(0, 0.05f)] public float _AttachmentLineWidth = 0.002f;

        [BoxGroup("Interaction Behaviour")]
        [Tooltip("During collision/contact between two emitter hosts, only trigger the emitter with the greatest surface rigidity, using an average of the two values.")]
        public bool _OnlyTriggerMostRigidSurface = true;

        [BoxGroup("Registered Elements")]
        public List<HostAuthoring> _Hosts = new ();
        [BoxGroup("Registered Elements")]
        public List<EmitterAuthoring> _Emitters = new();
        [BoxGroup("Registered Elements")]
        public List<SpeakerAuthoring> _Speakers = new ();
        
        #endregion

        #region UPDATE SCHEDULE

        private void Awake()
        {
            Instance = this;
            _SampleRate = AudioSettings.outputSampleRate;
            _SamplesPerMS = (int)(SampleRate * .001f);
            _SpeakerAllocationTimer = new Trigger(TimeUnit.seconds, _SpeakerAllocationPeriod);
            _MaxSpeakers = AudioSettings.GetConfiguration().numRealVoices;
            CheckSpeakerAllocation();
        }

        private void Start()
        {
            _Listener = FindObjectOfType<AudioListener>();

            if (_SpeakerParentTransform == null)
            {
                GameObject go = new($"Speakers");
                go.transform.parent = transform;
                go.transform.position = transform.position;
                _SpeakerParentTransform = go.transform;
            }

            _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _GrainQuery = _EntityManager.CreateEntityQuery(typeof(GrainComponent), typeof(SamplesProcessedTag));

            _WindowingEntity = UpdateEntity(_WindowingEntity, SynthComponentType.Windowing);
            _AudioTimerEntity = UpdateEntity(_AudioTimerEntity, SynthComponentType.AudioTimer);
            _AttachmentEntity = UpdateEntity(_AttachmentEntity, SynthComponentType.Connection);
        }

        private void Update()
        {
            _FrameStartSampleIndex = CurrentSampleIndex;
            _LastFrameSampleDuration = (int)(Time.deltaTime * SampleRate);

            CheckSpeakerAllocation();
            UpdateEntity(_AttachmentEntity, SynthComponentType.Connection);

            SpeakerUpkeep();
            UpdateSpeakers();
            DistributeGrains();

            UpdateHosts();
            UpdateEmitters();

            UpdateEntity(_AudioTimerEntity, SynthComponentType.AudioTimer);

            UpdateStatsUI();
        }

        #endregion

        #region COMPONENT UPDATES

        private Entity CreateEntity(string entityType)
        {
            Entity entity = _EntityManager.CreateEntity();

#if UNITY_EDITOR
            _EntityManager.SetName(entity, entityType);
#endif
            return entity;
        }

        private Entity UpdateEntity(Entity entity, string entityType)
        {
            if (!SynthComponentType.IsValid(entityType) || entityType == SynthComponentType.AudioClip)
                return entity;

            entity = entity != Entity.Null ? entity : CreateEntity(entityType);

            if (entityType == SynthComponentType.Windowing)
                PopulateWindowingEntity(entity);
            else if (entityType == SynthComponentType.Connection)
                PopulateConnectionEntity(entity);
            else if (entityType == SynthComponentType.AudioTimer)
                PopulateTimerEntity(entity);

            return entity;
        }

        private void PopulateTimerEntity(Entity entity)
        {
            if (_EntityManager.HasComponent<AudioTimerComponent>(entity))
                _EntityManager.SetComponentData(entity, new AudioTimerComponent
                {
                    _NextFrameIndexEstimate = NextFrameIndexEstimate,
                    _GrainQueueSampleDuration = QueueDurationSamples,
                    _PreviousFrameSampleDuration = _LastFrameSampleDuration,
                    _RandomiseBurstStartIndex = BurstStartOffsetRange,
                    _AverageGrainAge = (int)_AverageGrainAge
                });
            else
                _EntityManager.AddComponentData(entity, new AudioTimerComponent
                {
                    _NextFrameIndexEstimate = NextFrameIndexEstimate,
                    _GrainQueueSampleDuration = QueueDurationSamples,
                    _PreviousFrameSampleDuration = _LastFrameSampleDuration,
                    _RandomiseBurstStartIndex = BurstStartOffsetRange,
                    _AverageGrainAge = (int)_AverageGrainAge
                });
        }

        private void PopulateConnectionEntity(Entity entity)
        {
            if (_EntityManager.HasComponent<ConnectionConfig>(entity))
                _EntityManager.SetComponentData(entity, new ConnectionConfig
                {
                    _DeltaTime = Time.deltaTime,
                    _ListenerPos = _Listener.transform.position,
                    _ListenerRadius = _ListenerRadius,
                    _BusyLoadLimit = _SpeakerBusyLoadLimit,
                    _ArcDegrees = _SpeakerAttachArcDegrees,
                    _TranslationSmoothing = AttachSmoothing,
                    _DisconnectedPosition = _SpeakerPoolingPosition,
                    _SpeakerLingerTime = _SpeakerLingerTime / 1000
                });
            else
                _EntityManager.AddComponentData(entity, new ConnectionConfig
                {
                    _DeltaTime = 0,
                    _ListenerPos = _Listener.transform.position,
                    _ListenerRadius = _ListenerRadius,
                    _BusyLoadLimit = _SpeakerBusyLoadLimit,
                    _ArcDegrees = _SpeakerAttachArcDegrees,
                    _TranslationSmoothing = AttachSmoothing,
                    _DisconnectedPosition = _SpeakerPoolingPosition,
                    _SpeakerLingerTime = _SpeakerLingerTime / 1000
                });
        }

        private void PopulateWindowingEntity(Entity entity)
        {
            float[] window = _GrainEnvelope.BuildWindowArray();

            using BlobBuilder blobTheBuilder = new BlobBuilder(Allocator.Temp);
            ref FloatBlobAsset windowingBlobAsset = ref blobTheBuilder.ConstructRoot<FloatBlobAsset>();

            BlobBuilderArray<float> windowArray = blobTheBuilder.Allocate(ref windowingBlobAsset.array, _GrainEnvelope._EnvelopeSize);

            for (int i = 0; i < windowArray.Length; i++)
                windowArray[i] = window[i];

            BlobAssetReference<FloatBlobAsset> windowingBlobAssetRef = blobTheBuilder.CreateBlobAssetReference<FloatBlobAsset>(Allocator.Persistent);
            _EntityManager.AddComponentData(entity, new WindowingDataComponent { _WindowingArray = windowingBlobAssetRef });
        }

        #endregion

        #region PROCESS GRAINS

        private void DistributeGrains()
        {
            NativeArray<Entity> grainEntities = _GrainQuery.ToEntityArray(Allocator.TempJob);
            int grainCount = grainEntities.Length;
            _GrainsPerFrame = Mathf.Lerp(_GrainsPerFrame, grainCount, Time.deltaTime * 5);
            _GrainsPerSecond = Mathf.Lerp(_GrainsPerSecond, grainCount / Time.deltaTime, Time.deltaTime * 2);
            _GrainsPerSecondPeak = Math.Max(_GrainsPerSecondPeak, grainCount / Time.deltaTime);

            if (_Speakers.Count == 0 && grainCount > 0)
            {
                Debug.Log($"No speakers registered. Destroying {grainCount} grains.");
                _GrainsDiscarded += grainCount;
                grainEntities.Dispose();
                return;
            }

            GrainComponent grain;
            float ageSum = 0;

            for (int i = 0; i < grainCount; i++)
            {
                grain = _EntityManager.GetComponentData<GrainComponent>(grainEntities[i]);
                ageSum += _FrameStartSampleIndex - grain._StartSampleIndex;

                SpeakerAuthoring speaker = GetSpeakerForGrain(grain);

                if (speaker == null || grain._StartSampleIndex < GrainDiscardSampleIndex || 
                    speaker.GetEmptyGrain(out Grain grainOutput) == null)
                {
                    _EntityManager.DestroyEntity(grainEntities[i]);
                    _GrainsDiscarded++;
                    continue;
                }
                try
                {
                    NativeArray<float> grainSamples = _EntityManager.GetBuffer<GrainSampleBufferElement>(grainEntities[i]).Reinterpret<float>().ToNativeArray(Allocator.Temp);
                    NativeToManagedCopyMemory(grainOutput._SampleData, grainSamples);
                    grainOutput._Pooled = false;
                    grainOutput._IsPlaying = true;
                    grainOutput._PlayheadIndex = 0;
                    grainOutput._SizeInSamples = grainSamples.Length;
                    grainOutput._DSPStartTime = grain._StartSampleIndex;
                    grainOutput._PlayheadNormalised = grain._PlayheadNorm;
                    speaker.GrainAdded(grainOutput);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is NullReferenceException)
                {
                    Debug.LogWarning($"Error while copying grain to managed array for speaker ({grain._SpeakerIndex}). Destroying grain entity {i}.\n{ex}");
                }
                _EntityManager.DestroyEntity(grainEntities[i]);
            }
            grainEntities.Dispose();

            if (grainCount > 0)
            {
                _AverageGrainAge = Mathf.Lerp(_AverageGrainAge, ageSum / grainCount, Time.deltaTime * 5);
                _AverageGrainAgeMS = _AverageGrainAge / SamplesPerMS;
            }
        }

        public static unsafe void NativeToManagedCopyMemory(float[] targetArray, NativeArray<float> SourceNativeArray)
        {
            void* memoryPointer = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(SourceNativeArray);
            Marshal.Copy((IntPtr)memoryPointer, targetArray, 0, SourceNativeArray.Length);
        }

        #endregion

        #region SYNTH ELEMENT UPDATES

        public void UpdateSpeakers()
        {
            foreach (SpeakerAuthoring speaker in _Speakers)
                if (speaker != null)
                    speaker.PrimaryUpdate();
        }

        public void UpdateHosts()
        {
            TrimHostList();
            foreach (HostAuthoring host in _Hosts)
                if (host != null)
                    host.PrimaryUpdate();
        }

        public void UpdateEmitters()
        {
            TrimEmitterList();
            foreach (EmitterAuthoring emitter in _Emitters)
                if (emitter != null)
                    emitter.PrimaryUpdate();
        }


        #endregion

        #region SPEAKER MANAGEMENT

        public void CheckSpeakerAllocation()
        {
            if (_SpeakersAllocated > _MaxSpeakers)
            {
                Debug.Log($"Warning: Number of synth speakers ({_SpeakersAllocated}) cannot exceed number of audio voices {_MaxSpeakers} configured in the project settings.");
                _SpeakersAllocated = _MaxSpeakers;
            }
            _SpeakerAllocationTimer.UpdateTrigger(Time.deltaTime, _SpeakerAllocationPeriod);
        }

        public SpeakerAuthoring CreateSpeaker(int index)
        {
            SpeakerAuthoring newSpeaker = Instantiate(_SpeakerPrefab, _SpeakerPoolingPosition, Quaternion.identity, _SpeakerParentTransform);
            newSpeaker.SetIndex(index);
            newSpeaker.SetGrainArraySize(_SpeakerGrainArraySize);
            return newSpeaker;
        }

        public void DeregisterSpeaker(SpeakerAuthoring speaker)
        {
        }

        public void SpeakerUpkeep()
        {
            for (int i = 0; i < _Speakers.Count; i++)
            {
                if (_Speakers[i] == null)
                    _Speakers[i] = CreateSpeaker(i);
                if (_Speakers[i] != null && _Speakers[i].EntityIndex != i)
                    _Speakers[i].SetIndex(i);
                if (_Speakers[i] != null)
                    _Speakers[i].SetGrainArraySize(_SpeakerGrainArraySize);
            }
            while (_Speakers.Count < SpeakersAllocated && _SpeakerAllocationTimer.DrainTrigger())
            {
                _Speakers.Add(CreateSpeaker(_Speakers.Count - 1));
            }
            while (_Speakers.Count > SpeakersAllocated && _SpeakerAllocationTimer.DrainTrigger())
            {
                Destroy(_Speakers[_Speakers.Count - 1].gameObject);
                _Speakers.RemoveAt(_Speakers.Count - 1);
            }
        }

        public bool IsSpeakerAtIndex(int index, out SpeakerAuthoring speaker)
        {
            if (index >= _Speakers.Count)
                speaker = null;
            else
                speaker = _Speakers[index];
            return speaker != null;
        }

        public SpeakerAuthoring GetSpeakerForGrain(GrainComponent grain)
        {
            if (!IsSpeakerAtIndex(grain._SpeakerIndex, out SpeakerAuthoring speaker) ||
                grain._SpeakerIndex == int.MaxValue)
            {
                return null;
            }
            return speaker;
        }

        public int GetIndexOfSpeaker(SpeakerAuthoring speaker)
        {
            int index = _Speakers.IndexOf(speaker);
            if (index == -1 || index >= _Speakers.Count)
                return -1;
            return _Speakers.IndexOf(speaker);
        }

        #endregion

        #region SYNTH ENTITY REGISTRATION

        public int RegisterHost(HostAuthoring host)
        {
            for (int i = 0; i < _Hosts.Count; i++)
                if (_Hosts[i] == null)
                {
                    _Hosts[i] = host;
                    return i;
                }
            _Hosts.Add(host);
            return _Hosts.Count - 1;
        }

        public int RegisterEmitter(EmitterAuthoring emitter)
        {
            for (int i = 0; i < _Emitters.Count; i++)
                if (_Emitters[i] == null)
                {
                    _Emitters[i] = emitter;
                    return i;
                }
            _Emitters.Add(emitter);
            return _Emitters.Count - 1;
        }

        public void TrimHostList()
        {
            for (int i = _Hosts.Count - 1; i >= 0; i--)
            {
                if (_Hosts[i] == null)
                    _Hosts.RemoveAt(i);
                else return;
            }
        }

        public void TrimEmitterList()
        {
            for (int i = _Emitters.Count - 1; i >= 0; i--)
            {
                if (_Emitters[i] == null)
                    _Emitters.RemoveAt(i);
                else return;
            }
        }

        public void DeregisterHost(HostAuthoring host)
        {
        }

        public void DeregisterEmitter(EmitterAuthoring emitter)
        {
        }

        #endregion

        #region AUDIO DSP CLOCK

        void OnAudioFilterRead(float[] data, int channels)
        {
            for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
                _CurrentSampleIndex++;
        }

        #endregion

        #region STATS UI UPDATE

        public void UpdateStatsUI()
        {
            _SpeakerCount = Mathf.CeilToInt(Mathf.Lerp(_SpeakerCount, _Speakers.Count, Time.deltaTime * 10));
            _HostCount = Mathf.CeilToInt(Mathf.Lerp(_HostCount, _Hosts.Count, Time.deltaTime * 10));
            _EmitterCount = Mathf.CeilToInt(Mathf.Lerp(_EmitterCount, _Emitters.Count, Time.deltaTime * 10));

            if (_StatsValuesText != null)
            {
                _StatsValuesText.text = $"{(int)_GrainsPerSecond}\n{_GrainsDiscarded}\n{_AverageGrainAgeMS.ToString("F2")}";
                _StatsValuesText.text += $"\n{_SpeakerCount}\n{_HostCount}\n{_EmitterCount}";
            }
            if (_StatsValuesText != null)
            {
                _StatsValuesPeakText.text = $"{(int)_GrainsPerSecondPeak}";
            }

        }

        #endregion

        #region GIZMOS

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_Listener.transform.position, _ListenerRadius);
        }

        #endregion
    }
}