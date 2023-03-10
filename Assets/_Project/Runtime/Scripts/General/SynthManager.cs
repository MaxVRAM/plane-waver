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

using MaxVRAM.Counters;
using MaxVRAM.Audio;

using PlaneWaver.DSP;
using PlaneWaver.Emitters;

namespace PlaneWaver
{
    /// <summary>
    /// Single-instanced manager component that governs the synthesiser's entities and grain delivery.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SynthManager : MonoBehaviour
    {
        #region FIELDS & PROPERTIES

        public static SynthManager Instance;
        public bool EnableDebugCounters = true;
        private bool IsInitialised { get; set; }
        private EntityManager _entityManager;
        private EntityQuery _grainQuery;

        private Entity _windowingEntity;
        private Entity _timerEntity;
        private Entity _connectionEntity;

        private AudioListener _listener;
        public Transform ListenerTransform => _listener.transform;
        public float DistanceToListener (Transform other) => 
                Vector3.Distance(ListenerTransform.position, other.position);
        public float DistanceToListenerNorm (Transform other) => 
                Vector3.Distance(ListenerTransform.position, other.position) / ListenerRadius;

        private int _frameStartSampleIndex;
        private int _lastFrameSampleDuration;
        private float _grainsPerFrame;
        private float _grainsPerSecond;
        private float _grainsPerSecondPeak;
        private int _grainsDiscarded;
        private float _averageGrainAge;
        private float _averageGrainAgeMS;
        private int _speakerCount;
        private int _hostCount;
        private int _emitterCount;
        private int _frameCount;
        private int _maxSpeakers;

        [Tooltip("Maximum distance from the listener to enable emitters and allocate speakers.")]
        [BoxGroup("Audio Config")][Range(0.1f, 50)]
        public float ListenerRadius = 30;
        [Tooltip("Additional ms to calculate and queue grains each frame. Set to 0, the grainComponent queue equals the previous frame's duration. Adds latency, but help to avoid underrun. Recommended values > 20ms.")]
        [BoxGroup("Audio Config")][Range(0, 100)]
        public float QueueDurationMS = 22;
        [Tooltip("Percentage of previous frame duration to delay grainComponent start times of next frame. Adds a more predictable amount of latency to help avoid timing issues when the framerate slows.")]
        [BoxGroup("Audio Config")][Range(0, 100)]
        public float DelayPercentLastFrame = 10;
        [Tooltip("Discard un-played grains with a DSP start index more than this value (ms) in the past. Prevents clustered grainComponent playback when resources are near their limit.")]
        [BoxGroup("Audio Config")][Range(0, 60)]
        public float DiscardGrainsOlderThanMS = 10;
        [Tooltip("Delay bursts triggered on the same frame by a random amount. Helps avoid phasing issues caused by identical emitters triggered together.")]
        [BoxGroup("Audio Config")][Range(0, 40)]
        public float BurstStartOffsetRangeMS = 8;
        [Tooltip("Burst emitters ignore subsequent collisions for this duration to avoid fluttering from weird physics.")]
        [BoxGroup("Audio Config")][Range(0, 50)]
        public float BurstDebounceDurationMS = 25;
        [BoxGroup("Audio Config")]
        public Windowing GrainEnvelope;

        public int SampleRate { get; private set; } = 44100;
        public int SamplesPerMS { get; private set; } = 0;
        [field: ShowNonSerializedField]
        public int CurrentSampleIndex { get; private set; }
        public int QueueDurationSamples => (int)(QueueDurationMS * SamplesPerMS);
        public int BurstStartOffsetRange => (int)(BurstStartOffsetRangeMS * SamplesPerMS);
        public int GrainDiscardSampleIndex => _frameStartSampleIndex - (int)(DiscardGrainsOlderThanMS * SamplesPerMS);
        public int NextFrameIndexEstimate => _frameStartSampleIndex + (int)(_lastFrameSampleDuration * (1 + DelayPercentLastFrame / 100));
        
        [Tooltip("Target number of speakers to be spawned and managed by the synth system.")]
        [Range(0, 64)] public int SpeakerPoolCount = 16;
        [Tooltip("Period (seconds) to instantiate/destroy speakers. Affects performance only during start time or when altering the 'Speakers Allocated' value during runtime.")]
        [Range(0.01f, 1)] public float SpeakerAllocationPeriod = 0.2f;
        public CountTrigger SpeakerInstantiationTimer;
        [Tooltip("Speaker prefab to spawn when dynamically allocating speakers.")]
        public SpeakerAuthoring SpeakerPrefab;
        [Tooltip("ActorTransform to contain spawned speakers.")]
        public Transform SpeakerParentTransform;
        [Tooltip("World coordinates to store pooled speakers.")]
        public Vector3 SpeakerPoolingPosition = Vector3.down * 20;
        [Tooltip("Number of grains allocated to each speaker. Every frame the synth manager distributes grains to each grain's target speaker, which holds on to the grain object until all samples have been written to the output buffer.")]
        [Range(10, 200)] public int SpeakerGrainArraySize = 100;
        [Tooltip("The ratio of busy(?):(1)empty grains in each speaker before it is considered 'busy' and deprioritised as a target for additional emitters by the attachment system.")]
        [Range(0.1f, 45)] public float SpeakerBusyLoadLimit = 0.5f;
        [Tooltip("Arc length in degrees from the listener position that emitters can be attached to a speaker.")]
        [Range(0.1f, 45)] public float SpeakerAttachArcDegrees = 10;
        [Tooltip("How quickly speakers follow their targets. Increasing this value helps the speaker track its target, but can start invoking inappropriate doppler if tracking high numbers of ephemeral emitters.")]
        [Range(0, 50)] public float SpeakerTrackingSpeed = 20;
        [Tooltip("Length of time in milliseconds before pooling a speaker after its last emitter has disconnected. Allows speakers to be reused without destroying remaining grains from destroyed emitters.")]
        [Range(0, 500)] public float SpeakerLingerTime = 300;
        
        public int SpeakersAllocatedLimited => Math.Min(SpeakerPoolCount, _maxSpeakers);
        public bool PopulatingSpeakers => SpeakerPoolCount != Speakers.Count;
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
        public List<EmitterFrame> Frames = new ();
        [BoxGroup("Registered Elements")]
        public List<EmitterAuth> Emitters = new();
        [BoxGroup("Registered Elements")]
        public List<SpeakerAuthoring> Speakers = new ();
        
        #endregion

        #region INIT & VALIDATION

        private void Awake()
        {
            Instance = this;
            DefineAudioConfiguration();
            CheckSpeakerCountLimit();
        }

        private void Start()
        {
            ValidateSynthElements();
            
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _grainQuery = _entityManager.CreateEntityQuery(typeof(GrainComponent), typeof(SamplesProcessedTag));
            
            CreateSharedEntities();
            PopulateWindowingComponent();
        }
        
        private void DefineAudioConfiguration()
        {
            SampleRate = AudioSettings.outputSampleRate;
            SamplesPerMS = (int)(SampleRate * .001f);
            SpeakerInstantiationTimer = new CountTrigger(TimeUnit.Seconds, SpeakerAllocationPeriod);
            _maxSpeakers = AudioSettings.GetConfiguration().numRealVoices;
        }

        private void ValidateSynthElements()
        {
            _listener = FindObjectOfType<AudioListener>();
            
            if (_listener == null)
                throw new NullReferenceException("No AudioListener found in scene. Cannot create synth.");
            
            if (SpeakerPrefab == null)
                throw new NullReferenceException("Speaker prefab not set. Cannot start synth.");
            
            if (SpeakerParentTransform == null)
            {
                GameObject go = new($"Speakers");
                Transform speakerTransform = transform;
                go.transform.parent = speakerTransform;
                go.transform.position = speakerTransform.position;
                SpeakerParentTransform = go.transform;
            }
            
            IsInitialised = true;
        }
        
        #endregion

        #region UPDATE SCHEDULE
        
        private void Update()
        {
            if (!IsInitialised)
                return;
            
            _frameStartSampleIndex = CurrentSampleIndex;
            _lastFrameSampleDuration = (int)(Time.deltaTime * SampleRate);

            CheckSpeakerCountLimit();
            UpdateConnectionComponent();

            SpeakerUpkeep();
            UpdateSpeakers();
            DistributeGrains();

            UpdateFrames();
            UpdateTimerComponent();
            
            UpdateStatsUI();
        }

        #endregion

        #region COMPONENT UPDATES

        private void CreateSharedEntities()
        {
            _windowingEntity = _entityManager.CreateEntity(typeof(WindowingComponent));
            _connectionEntity = _entityManager.CreateEntity(typeof(ConnectionComponent));
            _timerEntity = _entityManager.CreateEntity(typeof(TimerComponent));
            
#if UNITY_EDITOR
            _entityManager.SetName(_windowingEntity, "_Windowing");
            _entityManager.SetName(_connectionEntity, "_Connection");
            _entityManager.SetName(_timerEntity, "_Timer");
#endif
        }
        
        private Entity CreateEntity(string entityType)
        {
            Entity entity = _entityManager.CreateEntity();

            return entity;
        }
        
        private void UpdateTimerComponent()
        {
            _entityManager.SetComponentData(_timerEntity, new TimerComponent
            {
                NextFrameIndexEstimate = NextFrameIndexEstimate,
                GrainQueueSampleDuration = QueueDurationSamples,
                PreviousFrameSampleDuration = _lastFrameSampleDuration,
                RandomiseBurstStartIndex = BurstStartOffsetRange,
                AverageGrainAge = (int)_averageGrainAge,
                SampleRate = SampleRate
            });
        }

        private void UpdateConnectionComponent()
        {
            _entityManager.SetComponentData(_connectionEntity, new ConnectionComponent
            {
                DeltaTime = Time.deltaTime,
                ListenerPos = _listener.transform.position,
                ListenerRadius = ListenerRadius,
                BusyLoadLimit = SpeakerBusyLoadLimit,
                ArcDegrees = SpeakerAttachArcDegrees,
                TranslationSmoothing = AttachSmoothing,
                DisconnectedPosition = SpeakerPoolingPosition,
                SpeakerLingerTime = SpeakerLingerTime * 0.001f
            });
        }

        private void PopulateWindowingComponent()
        {
            float[] window = GrainEnvelope.BuildWindowArray();

            using BlobBuilder blobTheBuilder = new BlobBuilder(Allocator.Temp);
            ref FloatBlobAsset windowingBlobAsset = ref blobTheBuilder.ConstructRoot<FloatBlobAsset>();

            BlobBuilderArray<float> windowArray = blobTheBuilder.Allocate(ref windowingBlobAsset.Array, GrainEnvelope.EnvelopeSize);

            for (int i = 0; i < windowArray.Length; i++)
                windowArray[i] = window[i];

            BlobAssetReference<FloatBlobAsset> windowingBlobAssetRef = blobTheBuilder.CreateBlobAssetReference<FloatBlobAsset>(Allocator.Persistent);
            _entityManager.SetComponentData(_windowingEntity, new WindowingComponent { WindowingArray = windowingBlobAssetRef });
        }

        #endregion

        #region PROCESS GRAINS

        private void DistributeGrains()
        {
            NativeArray<Entity> grainEntities = _grainQuery.ToEntityArray(Allocator.TempJob);
            int grainCount = grainEntities.Length;
            
            // Debug stats
            _grainsPerFrame = Mathf.Lerp(_grainsPerFrame, grainCount, Time.deltaTime * 5);
            _grainsPerSecond = Mathf.Lerp(_grainsPerSecond, grainCount / Time.deltaTime, Time.deltaTime * 2);
            _grainsPerSecondPeak = Math.Max(_grainsPerSecondPeak, grainCount / Time.deltaTime);
            // End debug stats
            
            if (Speakers.Count == 0 && grainCount > 0)
            {
                _grainsDiscarded += grainCount;
                grainEntities.Dispose();
                return;
            }

            float ageSum = 0;
            var grainsProcessed = 0;

            for (var i = 0; i < grainCount; i++)
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
                    grainsProcessed++;
                }
                catch (Exception ex) when (ex is ArgumentException || ex is NullReferenceException)
                {
                    Debug.LogWarning($"Error while copying grain to managed array for speaker ({grain.SpeakerIndex}). Destroying grain entity {i}.\n{ex}");
                }
                _entityManager.DestroyEntity(grainEntities[i]);
            }
            
            grainEntities.Dispose();
            _grainsDiscarded += grainCount - grainsProcessed;

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

        private void UpdateSpeakers()
        {
            foreach (SpeakerAuthoring speaker in Speakers.Where(speaker => speaker != null))
                speaker.PrimaryUpdate();
        }

        private void UpdateFrames()
        {
            TrimFrameList();

            foreach (EmitterFrame frame in Frames.Where(frame => frame != null))
                frame.PrimaryUpdate();
        }
        
        #endregion

        #region SPEAKER MANAGEMENT

        private void CheckSpeakerCountLimit()
        {
            SpeakerPoolCount = Mathf.Clamp(SpeakerPoolCount, 0, _maxSpeakers);
            SpeakerInstantiationTimer.UpdateTrigger(Time.deltaTime, SpeakerAllocationPeriod);
        }

        private SpeakerAuthoring CreateSpeaker(int index)
        {
            SpeakerAuthoring newSpeaker = Instantiate(
                SpeakerPrefab,
                SpeakerPoolingPosition,
                Quaternion.identity,
                SpeakerParentTransform
            );
            newSpeaker.SetGrainArraySize(SpeakerGrainArraySize);
            return newSpeaker;
        }
        
        public void DeregisterSpeaker(SpeakerAuthoring speaker)
        {
        }

        private void SpeakerUpkeep()
        {
            // Disabling this for the moment until I run into a situation where it's needed.
            // for (var i = 0; i < Speakers.Count; i++)
            // {
            //     if (Speakers[i] == null)
            //         Speakers[i] = CreateSpeaker(i);
            // }
            
            while (Speakers.Count < SpeakersAllocatedLimited && SpeakerInstantiationTimer.DrainTrigger())
            {
                Speakers.Add(CreateSpeaker(Speakers.Count - 1));
            }
            while (Speakers.Count > SpeakersAllocatedLimited && SpeakerInstantiationTimer.DrainTrigger())
            {
                Destroy(Speakers[^1].gameObject);
                Speakers.RemoveAt(Speakers.Count - 1);
            }
        }

        public bool ValidSpeakerAtIndex(int index, [NotNull] out SpeakerAuthoring speaker)
        {
            bool foundSpeaker = index >= 0 && index < Speakers.Count;
            speaker = Speakers[index];
            return foundSpeaker;
        }

        private SpeakerAuthoring GetSpeakerForGrain(GrainComponent grain)
        {
            return ValidSpeakerAtIndex(grain.SpeakerIndex, out SpeakerAuthoring speaker) ? speaker : null;
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
                SynthElementType.Frame   => RegisterFrame((EmitterFrame)synthElement),
                _                        => -1
            };
        }

        private int RegisterFrame(EmitterFrame frame)
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
        
        private void TrimFrameList()
        {
            for (int i = Frames.Count - 1; i >= 0; i--)
            {
                if (Frames[i] == null)
                    Frames.RemoveAt(i);
                else return;
            }
        }
        
        public void DeregisterFrame(EmitterFrame frame)
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

        private void UpdateStatsUI()
        {
            _speakerCount = Mathf.CeilToInt(Mathf.Lerp(_speakerCount, Speakers.Count, Time.deltaTime * 10));
            _hostCount = Mathf.CeilToInt(Mathf.Lerp(_hostCount, Frames.Count, Time.deltaTime * 10));
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
}