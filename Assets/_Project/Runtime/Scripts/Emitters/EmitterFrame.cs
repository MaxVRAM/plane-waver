using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using MaxVRAM.Extensions;
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

        private bool IsConnected => SpeakerTransform != SpeakerTarget;
        private bool InListenerRange { get; set; }
        public MaterialColourModulator MaterialModulator = new();

        private Transform _headTransform;

        #endregion

        #region INITIALISATION METHODS

        private void Start()
        {
            if (_actor == null)
                _actor = GetComponent<Actor>() ?? gameObject.AddComponent<Actor>();

            _actor.OnNewValidCollision += TriggerCollisionEmitters;

            InitialiseAttachmentPoint();
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

        private void InitialiseAttachmentPoint()
        {
            if (SpeakerTarget == null)
                SpeakerTarget = new GameObject("SpeakerTarget").SetParentAndZero(_actor.transform).transform;
            SpeakerTransform = SpeakerTarget;
        }

        private void InitialiseMaterialModulator()
        {
            if (MaterialModulator._Renderer == null && _actor != null)
                MaterialModulator._Renderer = _actor.GetComponentInChildren<Renderer>();
        }

        protected override void InitialiseComponents()
        {
            Manager.SetComponentData
            (
                ElementEntity,
                new FrameComponent
                {
                    Index = EntityIndex, Position = SpeakerTarget.position,
                }
            );
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
            Manager.SetComponentData
            (
                ElementEntity,
                new FrameComponent
                {
                    Index = EntityIndex, Position = SpeakerTarget.position
                }
            );
        }

        private void UpdateInRangeStatus()
        {
            InListenerRange = _actor.SpeakerTargetFromListenerNorm() < 1f;

            if (InListenerRange)
                Manager.AddComponent(ElementEntity, typeof(InListenerRangeTag));
            else
            {
                Manager.RemoveComponent(ElementEntity, typeof(InListenerRangeTag));
                RemoveSpeakerComponent();
            }
        }

        private void ValidateSpeakerComponent()
        {
            if (!InListenerRange)
                return;

            if (Manager.HasComponent(ElementEntity, typeof(FrameConnection)))
            {
                int index = Manager.GetComponentData<FrameConnection>(ElementEntity).SpeakerIndex;

                if (GrainBrain.Instance.IsSpeakerAtIndex(index, out SpeakerAuthoring speaker))
                {
                    SpeakerTransform = speaker.transform;
                    SpeakerIndex = index;
                    return;
                }
            }

            RemoveSpeakerComponent();
        }

        private void RemoveSpeakerComponent()
        {
            Manager.RemoveComponent<FrameConnection>(ElementEntity);
            SpeakerTransform = SpeakerTarget;
            SpeakerIndex = -1;
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

        #endregion
    }

    #region FRAME COMPONENTS

    public struct FrameComponent : IComponentData
    {
        public int Index;
        public float3 Position;
    }
    
    public struct FrameConnection : IComponentData
    {
        public int SpeakerIndex;
    }

    public struct InListenerRangeTag : IComponentData { }

    public struct AloneOnSpeakerTag : IComponentData { }

    #endregion
}