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

        public ActorObject Actor;

        [Header("Speaker Attachment")]
        [Tooltip("Parent transform for speakers to target. Defaults to this frame's Actor's transform.")]
        public Transform SpeakerTarget;
        [Tooltip("Assigned to the speaker's transform when connected, otherwise the frame's Speaker Target.")]
        public Transform SpeakerTransform;
        public int SpeakerIndex;

        public List<StableEmitterAuth> StableEmitters = new();
        public List<VolatileEmitterAuth> VolatileEmitters = new();
        
        public bool IsConnected;
        public bool CheckConnection => SpeakerTransform != null && SpeakerTransform != SpeakerTarget;
        public bool InListenerRange;
        public MaterialColourModulator MaterialModulator = new();

        private Transform _headTransform;

        #endregion

        #region INITIALISATION METHODS

        private void Start()
        {
            if (Actor == null)
                Actor = GetComponent<ActorObject>() ?? gameObject.AddComponent<ActorObject>();

            Actor.OnNewValidCollision += TriggerCollisionEmitters;

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
                if (StableEmitters[i] != null)
                    StableEmitters[i].Initialise(i, name, in Actor);
            
            for (var i = 0; i < VolatileEmitters.Count; i++)
                if (VolatileEmitters[i] != null)
                    VolatileEmitters[i].Initialise(i, name, in Actor);
            
            // StableEmitters.RemoveAll(e => e == null);
            // VolatileEmitters.RemoveAll(e => e == null);
        }

        private void InitialiseSpeakerTarget()
        {
            SpeakerTarget = Actor != null && Actor.SpeakerTarget != null ? Actor.SpeakerTarget : transform;
            SpeakerTransform = SpeakerTarget;
        }

        private void InitialiseMaterialModulator()
        {
            if (MaterialModulator._Renderer == null && Actor != null)
                MaterialModulator._Renderer = Actor.GetComponentInChildren<Renderer>();
        }

        protected override void InitialiseComponents()
        {
            Manager.SetComponentData(ElementEntity, new FrameComponent { 
                        Index = EntityIndex, Position = SpeakerTarget.position });
        }

        private void OnDisable() { Actor.OnNewValidCollision -= TriggerCollisionEmitters; }

        #endregion

        #region RUNTIME UPDATES

        protected override void ProcessComponents()
        {
            UpdatePosition();
            UpdateInRangeStatus();
            CheckSpeakerAttachment();
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
            InListenerRange = Actor.SpeakerTargetToListenerNorm() < 1f;

            if (InListenerRange)
                Manager.AddComponent(ElementEntity, typeof(InListenerRangeTag));
            else
            {
                Manager.RemoveComponent(ElementEntity, typeof(InListenerRangeTag));
                RemoveConnectionComponent();
            }
        }

        private void CheckSpeakerAttachment()
        {
            if (!InListenerRange)
                    return;
            
            if (!Manager.HasComponent(ElementEntity, typeof(SpeakerConnection)))
            {
                IsConnected = false;
                return;
            }

            int index = Manager.GetComponentData<SpeakerConnection>(ElementEntity).SpeakerIndex;
            
            if (SynthManager.Instance.ValidSpeakerAtIndex(index, out SynthSpeaker speaker))
            {
                SpeakerTransform = speaker.transform;
                SpeakerIndex = index;
                IsConnected = true;
            }
            else
            {
                Debug.Log($"{name} removing speaker connection component with index {index}.");
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
            foreach (StableEmitterAuth emitter in StableEmitters)
                emitter.UpdateEmitterEntity(InListenerRange, IsConnected, SpeakerIndex);

            foreach (VolatileEmitterAuth emitter in VolatileEmitters)
                emitter.UpdateEmitterEntity(InListenerRange, IsConnected, SpeakerIndex);
        }

        private void TriggerCollisionEmitters(CollisionData data)
        {
            foreach (VolatileEmitterAuth emitter in VolatileEmitters)
                emitter.ApplyNewCollision(data);
        }

        protected override void Deregister()
        {
            Debug.Log($"Deregistering {name}.");
            foreach (StableEmitterAuth emitter in StableEmitters)
                emitter.OnDestroy();
            foreach (VolatileEmitterAuth emitter in VolatileEmitters)
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