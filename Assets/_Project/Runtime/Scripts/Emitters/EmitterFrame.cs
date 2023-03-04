using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

using PlaneWaver.DSP;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    public class EmitterFrame : SynthElement
    {
        #region CLASS DEFINITIONS

        private Actor _actor;
        public Actor Actor { get => _actor; set => _actor = value; }

        [Header("Speaker Attachment")]
        [Tooltip("Parent transform for speakers to target. Defaults to this frame's Actor's transform.")]
        public Transform SpeakerTarget;
        [Tooltip("Assigned to the speaker's transform when connected, otherwise the frame's Speaker Target.")]
        public Transform SpeakerTransform;
        public int SpeakerIndex;

        public List<EmitterAuth> StableEmitters = new();
        public List<EmitterAuth> VolatileEmitters = new();

        public bool IsConnected;
        public bool CheckConnection => SpeakerTransform != null && SpeakerTransform != SpeakerTarget;
        public bool InListenerRange;
        public MaterialColourModulator MaterialModulator = new();

        private Transform _headTransform;

        #endregion

        #region INITIALISATION METHODS

        private void Start()
        {
            if (_actor == null)
                _actor = GetComponent<Actor>() ?? gameObject.AddComponent<Actor>();

            _actor.OnNewValidCollision += TriggerCollisionEmitters;

            InitialiseSpeakerTarget();
            InitialiseMaterialModulator();

            _headTransform = FindObjectOfType<Camera>().transform;

            ElementType = SynthElementType.Frame;
            Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            ElementArchetype = Manager.CreateArchetype(typeof(FrameComponent));
            InitialiseEntity();
            InitialiseEmitters();
        }

        private void InitialiseEmitters()
        {
            for (var i = 0; i < StableEmitters.Count; i++)
            {
                if (StableEmitters[i] == null || StableEmitters[i].EmitterAsset == null)
                    StableEmitters.RemoveAt(i);
                else
                    StableEmitters[i].Initialise(i, name, in _actor);
            }

            for (var i = 0; i < VolatileEmitters.Count; i++)
            {
                if (VolatileEmitters[i] == null || VolatileEmitters[i].EmitterAsset == null)
                    VolatileEmitters.RemoveAt(i);
                else
                    VolatileEmitters[i].Initialise(i, name, in _actor);
            }
        }

        private void InitialiseSpeakerTarget()
        {
            SpeakerTarget = _actor != null && _actor.SpeakerTarget != null ? _actor.SpeakerTarget : transform;
            SpeakerTransform = SpeakerTarget;
        }

        private void InitialiseMaterialModulator()
        {
            if (MaterialModulator._Renderer == null && _actor != null)
                MaterialModulator._Renderer = _actor.GetComponentInChildren<Renderer>();
        }

        protected override void InitialiseComponents()
        {
            Manager.SetComponentData(ElementEntity, new FrameComponent { 
                        Index = EntityIndex, Position = SpeakerTarget.position });
        }

        private void OnDisable() { _actor.OnNewValidCollision -= TriggerCollisionEmitters; }

        #endregion

        #region RUNTIME UPDATES

        protected override void ProcessComponents()
        {
            UpdatePosition();
            UpdateInRangeStatus();
            ValidateSpeakerComponent();
            UpdateEmitters();
        }

        private void UpdatePosition()
        {
            Manager.SetComponentData(ElementEntity, new FrameComponent {
                Index = EntityIndex, Position = SpeakerTarget.position
            });
        }

        private void UpdateInRangeStatus()
        {
            InListenerRange = _actor.SpeakerTargetToListenerNorm() < 1f;

            if (InListenerRange)
                Manager.AddComponent(ElementEntity, typeof(InListenerRangeTag));
            else
            {
                Manager.RemoveComponent(ElementEntity, typeof(InListenerRangeTag));
                RemoveConnectionComponent();
            }
        }

        private void ValidateSpeakerComponent()
        {
            if (!InListenerRange)
                    return;
            
            if (!Manager.HasComponent(ElementEntity, typeof(SpeakerConnection)))
            {
                IsConnected = false;
                return;
            }

            int index = Manager.GetComponentData<SpeakerConnection>(ElementEntity).SpeakerIndex;
            
            if (GrainBrain.Instance.IsSpeakerAtIndex(index, out SpeakerAuthoring speaker))
            {
                SpeakerTransform = speaker.transform;
                SpeakerIndex = index;
                IsConnected = true;
            }
            else
            {
                Debug.Log($"{name} removing invalid connection component with index {index}.");
                RemoveConnectionComponent();
            }
        }

        private void RemoveConnectionComponent()
        {
            Manager.RemoveComponent<AloneOnSpeakerTag>(ElementEntity);
            Manager.RemoveComponent<SpeakerConnection>(ElementEntity);
            SpeakerTransform = SpeakerTarget;
            SpeakerIndex = -1;
            IsConnected = false;
        }

        private void UpdateEmitters()
        {
            foreach (EmitterAuth emitter in StableEmitters)
                emitter.UpdateEmitterEntity(InListenerRange, IsConnected, SpeakerIndex);

            foreach (EmitterAuth emitter in VolatileEmitters)
                emitter.UpdateEmitterEntity(InListenerRange, IsConnected, SpeakerIndex);
        }

        private void TriggerCollisionEmitters(CollisionData data)
        {
            foreach (EmitterAuth emitter in VolatileEmitters)
                emitter.ApplyNewCollision(data);
        }

        protected override void Deregister()
        {
            foreach (EmitterAuth emitter in StableEmitters)
                emitter.OnDestroy();
            foreach (EmitterAuth emitter in VolatileEmitters)
                emitter.OnDestroy();
        }

        #endregion
    }

    #region FRAME COMPONENTS

    public struct FrameComponent : IComponentData
    {
        public int Index;
        public float3 Position;
    }
    
    public struct SpeakerConnection : IComponentData
    {
        public int SpeakerIndex;
    }

    public struct InListenerRangeTag : IComponentData { }

    public struct AloneOnSpeakerTag : IComponentData { }

    #endregion
}