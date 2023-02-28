using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using TMPro;

using NaughtyAttributes;

using MaxVRAM.Ticker;
using MaxVRAM.Audio;

using PlaneWaver.DSP;
using PlaneWaver.Emitters;

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

        private EntityManager _entityManager;
        private EntityQuery _grainQuery;

        private Entity _windowingEntity;
        private Entity _audioTimerEntity;
        private Entity _attachmentEntity;

        private AudioListener _listener;
        public Transform ListenerTransform => _listener.transform;
        public Vector3 ListenerPosition => _listener.transform.position;

        private int _frameStartSampleIndex = 0;
        private int _lastFrameSampleDuration = 0;
        private float _grainsPerFrame = 0;
        private float _grainsPerSecond = 0;
        private float _grainsPerSecondPeak = 0;
        private int _grainsDiscarded = 0;
        private float _averageGrainAge = 0;
        private float _averageGrainAgeMS = 0;
        private int _speakerCount;
        private int _hostCount;
        private int _emitterCount;
        private int _frameCount;

        [Tooltip("Maximum number of speakers possible, defined by 'numRealVoices' in the project settings audio tab.")]
        [ShowNonSerializedField] private int _maxSpeakers = 0;

        [Tooltip("Maximum distance from the listener to enable emitters and allocate speakers.")]
        [BoxGroup("Audio Config")][Range(0.1f, 50)]
        public float ListenerRadius = 30;
        [Tooltip("Additional ms to calculate and queue grains each frame. Set to 0, the grainComponent queue equals the previous frame's duration. Adds latency, but help to avoid underrun. Recommended values > 20ms.")]
        [BoxGroup("Audio Config")][Range(0, 100)]
        public float QueueDurationMS = 22;
        [Tooltip("Percentage of previous frame duration to delay grainComponent start times of next frame. Adds a more predictable amount of latency to help avoid timing issues when the framerate slows.")]
        [BoxGroup("Audio Config")][Range(0, 100)]
        public float DelayPercentLastFrame = 10;
        [Tooltip("Discard unplayed grains with a DSP start index more than this value (ms) in the past. Prevents clustered grainComponent playback when resources are near their limit.")]
        [BoxGroup("Audio Config")][Range(0, 60)]
        public float DiscardGrainsOlderThanMS = 10;
        [Tooltip("Delay bursts triggered on the same frame by a random amount. Helps avoid phasing issues caused by identical emitters triggered together.")]
        [BoxGroup("Audio Config")][Range(0, 40)]
        public float BurstStartOffsetRangeMS = 8;
        [Tooltip("Burst emitters ignore subsequent collisions for this duration to avoid fluttering from weird physics.")]
        [BoxGroup("Audio Config")][Range(0, 50)]
        public float BurstDebounceDurationMS = 25;
        [BoxGroup("Audio Config")][SerializeField]
        private Windowing GrainEnvelope;

        public int SampleRate { get; private set; } = 44100;
        public int SamplesPerMS { get; private set; } = 0;
        public int CurrentSampleIndex { get; private set; } = 0;
        public int QueueDurationSamples => (int)(QueueDurationMS * SamplesPerMS);
        public int BurstStartOffsetRange => (int)(BurstStartOffsetRangeMS * SamplesPerMS);
        public int GrainDiscardSampleIndex => _frameStartSampleIndex - (int)(DiscardGrainsOlderThanMS * SamplesPerMS);
        public int NextFrameIndexEstimate => _frameStartSampleIndex + (int)(_lastFrameSampleDuration * (1 + DelayPercentLastFrame / 100));
        
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Target number of speakers to be spawned and managed by the synth system.")]
        [Range(0, 255)][SerializeField] private int SpeakersAllocated = 32;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Period (seconds) to instantiate/destroy speakers. Affects performance only during start time or when altering the 'Speakers Allocated' value during runtime.")]
        [Range(0.01f, 1)][SerializeField] private float SpeakerAllocationPeriod = 0.2f;
        private Trigger _speakerAllocationTimer;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Speaker prefab to spawn when dynamically allocating speakers.")]
        public SpeakerAuthoring SpeakerPrefab;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("ActorTransform to contain spawned speakers.")]
        [SerializeField] private Transform SpeakerParentTransform;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("World coordinates to store pooled speakers.")]
        [SerializeField] private Vector3 SpeakerPoolingPosition = Vector3.down * 20;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Number of grains allocated to each speaker. Every frame the synth manager distributes grains to each grain's target speaker, which holds on to the grain object until all samples have been written to the output buffer.")]
        [Range(0, 255)][SerializeField] private int SpeakerGrainArraySize = 100;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("The ratio of busy(?):(1)empty grains in each speaker before it is considered 'busy' and deprioritised as a target for additional emitters by the attachment system.")]
        [Range(0.1f, 45)] public float SpeakerBusyLoadLimit = 0.5f;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Arc length in degrees from the listener position that emitters can be attached to a speaker.")]
        [Range(0.1f, 45)] public float SpeakerAttachArcDegrees = 10;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("How quickly speakers follow their targets. Increasing this value helps the speaker track its target, but can start invoking inappropriate doppler if tracking high numbers of ephemeral emitters.")]
        [Range(0, 50)] public float SpeakerTrackingSpeed = 20;
        [BoxGroup("Speaker Configuration")]
        [Tooltip("Length of time in milliseconds before pooling a speaker after its last emitter has disconnected. Allows speakers to be reused without destroying remaining grains from destroyed emitters.")]
        [Range(0, 500)] public float SpeakerLingerTime = 100;
        public int SpeakersAllocatedLimited => Math.Min(SpeakersAllocated, _maxSpeakers);
        public bool PopulatingSpeakers => SpeakersAllocated != Speakers.Count;
        public float AttachSmoothing => Mathf.Clamp(Time.deltaTime * SpeakerTrackingSpeed, 0, 1);

        [BoxGroup("Visual Feedback")]
        public TextMeshProUGUI StatsValuesText;
        [BoxGroup("Visual Feedback")]
        public TextMeshProUGUI StatsValuesPeakText;
        [BoxGroup("Visual Feedback")]
        public Material AttachmentLineMat;
        [BoxGroup("Visual Feedback")]
        [Range(0, 0.02f)] public float AttachmentLineWidth = 0.005f;

        [BoxGroup("Interaction Behaviour")]
        [Tooltip("During collision/contact between two emitter hosts, only trigger the emitter with the greatest surface rigidity, using an average of the two values.")]
        public bool OnlyTriggerMostRigidSurface = true;

        [BoxGroup("Registered Elements")]
        public List<GrainFrame> Frames = new ();
        [BoxGroup("Registered Elements")]
        public List<HostAuthoring> Hosts = new ();
        [BoxGroup("Registered Elements")]
        public List<EmitterAuthoring> Emitters = new();
        [BoxGroup("Registered Elements")]
        public List<SpeakerAuthoring> Speakers = new ();
        
        #endregion

        #region UPDATE SCHEDULE

        private void Awake()
        {
            Instance = this;
            SampleRate = AudioSettings.outputSampleRate;
            SamplesPerMS = (int)(SampleRate * .001f);
            _speakerAllocationTimer = new Trigger(TimeUnit.seconds, SpeakerAllocationPeriod);
            _maxSpeakers = AudioSettings.GetConfiguration().numRealVoices;
            CheckSpeakerAllocation();
        }

        private void Start()
        {
            _listener = FindObjectOfType<AudioListener>();
            
            if (_listener == null)
            {
                Debug.LogError("No AudioListener found in scene.");
                return;
            }
            
            if (SpeakerParentTransform == null)
            {
                GameObject go = new($"Speakers");
                Transform speakerTransform = transform;
                go.transform.parent = speakerTransform;
                go.transform.position = speakerTransform.position;
                SpeakerParentTransform = go.transform;
            }

            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _grainQuery = _entityManager.CreateEntityQuery(typeof(GrainComponent), typeof(SamplesProcessedTag));

            _windowingEntity = UpdateEntity(_windowingEntity, BrainComponentType.Windowing);
            _audioTimerEntity = UpdateEntity(_audioTimerEntity, BrainComponentType.AudioTimer);
            _attachmentEntity = UpdateEntity(_attachmentEntity, BrainComponentType.Connection);
        }

        private void Update()
        {
            _frameStartSampleIndex = CurrentSampleIndex;
            _lastFrameSampleDuration = (int)(Time.deltaTime * SampleRate);

            CheckSpeakerAllocation();
            UpdateEntity(_attachmentEntity, BrainComponentType.Connection);

            SpeakerUpkeep();
            UpdateSpeakers();
            DistributeGrains();

            UpdateHosts();
            UpdateEmitters();

            UpdateEntity(_audioTimerEntity, BrainComponentType.AudioTimer);

            UpdateStatsUI();
        }

        #endregion

        #region COMPONENT UPDATES

        private Entity CreateEntity(string entityType)
        {
            Entity entity = _entityManager.CreateEntity();

#if UNITY_EDITOR
            _entityManager.SetName(entity, entityType);
#endif
            return entity;
        }

        private Entity UpdateEntity(Entity entity, string entityType)
        {
            if (!BrainComponentType.IsValid(entityType))
                return entity;

            entity = entity != Entity.Null ? entity : CreateEntity(entityType);

            if (entityType == BrainComponentType.Windowing)
                PopulateWindowingEntity(entity);
            else if (entityType == BrainComponentType.Connection)
                PopulateConnectionEntity(entity);
            else if (entityType == BrainComponentType.AudioTimer)
                PopulateTimerEntity(entity);

            return entity;
        }

        private void PopulateTimerEntity(Entity entity)
        {
            _entityManager.AddComponentData(entity, new AudioTimerComponent
            {
                NextFrameIndexEstimate = NextFrameIndexEstimate,
                GrainQueueSampleDuration = QueueDurationSamples,
                PreviousFrameSampleDuration = _lastFrameSampleDuration,
                RandomiseBurstStartIndex = BurstStartOffsetRange,
                AverageGrainAge = (int)_averageGrainAge
            });
        }

        private void PopulateConnectionEntity(Entity entity)
        {
            _entityManager.AddComponentData(entity, new ConnectionConfig
            {
                DeltaTime = 0,
                ListenerPos = _listener.transform.position,
                ListenerRadius = ListenerRadius,
                BusyLoadLimit = SpeakerBusyLoadLimit,
                ArcDegrees = SpeakerAttachArcDegrees,
                TranslationSmoothing = AttachSmoothing,
                DisconnectedPosition = SpeakerPoolingPosition,
                SpeakerLingerTime = SpeakerLingerTime / 1000
            });
        }

        private void PopulateWindowingEntity(Entity entity)
        {
            float[] window = GrainEnvelope.BuildWindowArray();

            using BlobBuilder blobTheBuilder = new BlobBuilder(Allocator.Temp);
            ref FloatBlobAsset windowingBlobAsset = ref blobTheBuilder.ConstructRoot<FloatBlobAsset>();

            BlobBuilderArray<float> windowArray = blobTheBuilder.Allocate(ref windowingBlobAsset.Array, GrainEnvelope.EnvelopeSize);

            for (int i = 0; i < windowArray.Length; i++)
                windowArray[i] = window[i];

            BlobAssetReference<FloatBlobAsset> windowingBlobAssetRef = blobTheBuilder.CreateBlobAssetReference<FloatBlobAsset>(Allocator.Persistent);
            _entityManager.AddComponentData(entity, new WindowingDataComponent { WindowingArray = windowingBlobAssetRef });
        }

        #endregion

        #region PROCESS GRAINS

        private void DistributeGrains()
        {
            NativeArray<Entity> grainEntities = _grainQuery.ToEntityArray(Allocator.TempJob);
            int grainCount = grainEntities.Length;
            _grainsPerFrame = Mathf.Lerp(_grainsPerFrame, grainCount, Time.deltaTime * 5);
            _grainsPerSecond = Mathf.Lerp(_grainsPerSecond, grainCount / Time.deltaTime, Time.deltaTime * 2);
            _grainsPerSecondPeak = Math.Max(_grainsPerSecondPeak, grainCount / Time.deltaTime);

            if (Speakers.Count == 0 && grainCount > 0)
            {
                Debug.Log($"No speakers registered. Destroying {grainCount} grains.");
                _grainsDiscarded += grainCount;
                grainEntities.Dispose();
                return;
            }

            float ageSum = 0;

            for (int i = 0; i < grainCount; i++)
            {
                var grain = _entityManager.GetComponentData<GrainComponent>(grainEntities[i]);
                ageSum += _frameStartSampleIndex - grain.StartSampleIndex;

                SpeakerAuthoring speaker = GetSpeakerForGrain(grain);

                if (speaker == null || grain.StartSampleIndex < GrainDiscardSampleIndex || 
                    speaker.GetEmptyGrain(out Grain grainOutput) == null)
                {
                    _entityManager.DestroyEntity(grainEntities[i]);
                    _grainsDiscarded++;
                    continue;
                }
                try
                {
                    NativeArray<float> grainSamples = _entityManager.GetBuffer<GrainSampleBufferElement>(grainEntities[i]).Reinterpret<float>().ToNativeArray(Allocator.Temp);
                    NativeToManagedCopyMemory(grainOutput.SampleData, grainSamples);
                    grainOutput.Pooled = false;
                    grainOutput.IsPlaying = true;
                    grainOutput.PlayheadIndex = 0;
                    grainOutput.SizeInSamples = grainSamples.Length;
                    grainOutput.DSPStartTime = grain.StartSampleIndex;
                    grainOutput.PlayheadNormalised = grain.PlayheadNorm;
                    speaker.GrainAdded(grainOutput);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is NullReferenceException)
                {
                    Debug.LogWarning($"Error while copying grain to managed array for speaker ({grain.SpeakerIndex}). Destroying grain entity {i}.\n{ex}");
                }
                _entityManager.DestroyEntity(grainEntities[i]);
            }
            grainEntities.Dispose();

            if (grainCount <= 0) return;
            
            _averageGrainAge = Mathf.Lerp(_averageGrainAge, ageSum / grainCount, Time.deltaTime * 5);
            _averageGrainAgeMS = _averageGrainAge / SamplesPerMS;
        }

        private static unsafe void NativeToManagedCopyMemory(float[] targetArray, NativeArray<float> sourceNativeArray)
        {
            void* memoryPointer = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(sourceNativeArray);
            Marshal.Copy((IntPtr)memoryPointer, targetArray, 0, sourceNativeArray.Length);
        }

        #endregion

        #region SYNTH ELEMENT UPDATES

        public void UpdateSpeakers()
        {
            foreach (SpeakerAuthoring speaker in Speakers)
                if (speaker != null)
                    speaker.PrimaryUpdate();
        }

        public void UpdateHosts()
        {
            TrimHostList();
            foreach (HostAuthoring host in Hosts)
                if (host != null)
                    host.PrimaryUpdate();
        }

        public void UpdateEmitters()
        {
            TrimEmitterList();
            foreach (EmitterAuthoring emitter in Emitters)
                if (emitter != null)
                    emitter.PrimaryUpdate();
        }


        #endregion

        #region SPEAKER MANAGEMENT

        private void CheckSpeakerAllocation()
        {
            if (SpeakersAllocated > _maxSpeakers)
            {
                SpeakersAllocated = _maxSpeakers;
            }
            _speakerAllocationTimer.UpdateTrigger(Time.deltaTime, SpeakerAllocationPeriod);
        }

        public SpeakerAuthoring CreateSpeaker(int index)
        {
            SpeakerAuthoring newSpeaker = Instantiate(SpeakerPrefab,
                                                      SpeakerPoolingPosition,
                                                      Quaternion.identity,
                                                      SpeakerParentTransform);
            // newSpeaker.SetIndex(index);
            newSpeaker.SetGrainArraySize(SpeakerGrainArraySize);
            return newSpeaker;
        }
        
        public void DeregisterSpeaker(SpeakerAuthoring speaker)
        {
        }

        private void SpeakerUpkeep()
        {
            for (var i = 0; i < Speakers.Count; i++)
            {
                if (Speakers[i] == null)
                    Speakers[i] = CreateSpeaker(i);
            }
            while (Speakers.Count < SpeakersAllocatedLimited && _speakerAllocationTimer.DrainTrigger())
            {
                Speakers.Add(CreateSpeaker(Speakers.Count - 1));
            }
            while (Speakers.Count > SpeakersAllocatedLimited && _speakerAllocationTimer.DrainTrigger())
            {
                Destroy(Speakers[^1].gameObject);
                Speakers.RemoveAt(Speakers.Count - 1);
            }
        }

        public bool IsSpeakerAtIndex(int index, [NotNull] out SpeakerAuthoring speaker)
        {
            bool foundSpeaker = index >= 0 && index < Speakers.Count;
            speaker = Speakers[index];
            return foundSpeaker;
        }

        private SpeakerAuthoring GetSpeakerForGrain(GrainComponent grain)
        {
            if (!IsSpeakerAtIndex(grain.SpeakerIndex, out SpeakerAuthoring speaker) ||
                grain.SpeakerIndex == int.MaxValue)
            {
                return null;
            }
            return speaker;
        }

        public int GetIndexOfSpeaker(SpeakerAuthoring speaker)
        {
            int index = Speakers.IndexOf(speaker);
            if (index == -1 || index >= Speakers.Count)
                return -1;
            return Speakers.IndexOf(speaker);
        }

        #endregion

        #region SYNTH ENTITY REGISTRATION

        public int RegisterEntity(SynthElement synthElement, SynthElementType type)
        {
            return type switch
            {
                    SynthElementType.Speaker => GetIndexOfSpeaker((SpeakerAuthoring)synthElement),
                    SynthElementType.Frame   => RegisterFrame((GrainFrame)synthElement),
                    SynthElementType.Host    => RegisterHost((HostAuthoring)synthElement),
                    SynthElementType.Emitter => RegisterEmitter((EmitterAuthoring)synthElement),
                    _                        => -1
            };
        }
        
        public int RegisterFrame(GrainFrame frame)
        {
            for (var i = 0; i < Frames.Count; i++)
                if (Frames[i] == null)
                {
                    Frames[i] = frame;
                    return i;
                }
            Frames.Add(frame);
            return Frames.Count - 1;
        }
        
        public int RegisterHost(HostAuthoring host)
        {
            for (var i = 0; i < Hosts.Count; i++)
                if (Hosts[i] == null)
                {
                    Hosts[i] = host;
                    return i;
                }
            Hosts.Add(host);
            return Hosts.Count - 1;
        }

        public int RegisterEmitter(EmitterAuthoring emitter)
        {
            for (var i = 0; i < Emitters.Count; i++)
                if (Emitters[i] == null)
                {
                    Emitters[i] = emitter;
                    return i;
                }
            Emitters.Add(emitter);
            return Emitters.Count - 1;
        }
        
        private void TrimFrameList()
        {
            for (int i = Frames.Count - 1; i >= 0; i--)
            {
                if (Frames[i] == null)
                    Frames.RemoveAt(i);
                else return;
            }
        }
        
        private void TrimHostList()
        {
            for (int i = Hosts.Count - 1; i >= 0; i--)
            {
                if (Hosts[i] == null)
                    Hosts.RemoveAt(i);
                else return;
            }
        }

        private void TrimEmitterList()
        {
            for (int i = Emitters.Count - 1; i >= 0; i--)
            {
                if (Emitters[i] == null)
                    Emitters.RemoveAt(i);
                else return;
            }
        }

        public void DeregisterFrame(GrainFrame frame)
        {
        }
        
        public void DeregisterHost(HostAuthoring host)
        {
        }

        public void DeregisterEmitter(EmitterAuthoring emitter)
        {
        }

        #endregion

        #region AUDIO DSP CLOCK

        private void OnAudioFilterRead(float[] data, int channels)
        {
            for (var dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
                CurrentSampleIndex++;
        }

        #endregion

        #region STATS UI UPDATE

        public void UpdateStatsUI()
        {
            _speakerCount = Mathf.CeilToInt(Mathf.Lerp(_speakerCount, Speakers.Count, Time.deltaTime * 10));
            _hostCount = Mathf.CeilToInt(Mathf.Lerp(_hostCount, Hosts.Count, Time.deltaTime * 10));
            _emitterCount = Mathf.CeilToInt(Mathf.Lerp(_emitterCount, Emitters.Count, Time.deltaTime * 10));

            if (StatsValuesText != null)
            {
                StatsValuesText.text = $"{(int)_grainsPerSecond}\n{_grainsDiscarded}\n{_averageGrainAgeMS.ToString("F2")}";
                StatsValuesText.text += $"\n{_speakerCount}\n{_hostCount}\n{_emitterCount}";
            }
            if (StatsValuesText != null)
            {
                StatsValuesPeakText.text = $"{(int)_grainsPerSecondPeak}";
            }

        }

        #endregion

        #region GIZMOS

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_listener.transform.position, ListenerRadius);
        }

        #endregion
    }
    
    #region BRAIN COMPONENT TYPES
        
    public static class BrainComponentType
    {
        public const string Connection = "_AttachmentParameters";
        public const string Windowing = "_WindowingBlob";
        public const string AudioTimer = "_AudioTimer";

        public static bool IsValid(string entityType)
        {
            return typeof(BrainComponentType)
                  .GetFields()
                  .Any(field => field
                               .GetValue(null)
                               .ToString() == entityType);
        }
    }

    #endregion
}