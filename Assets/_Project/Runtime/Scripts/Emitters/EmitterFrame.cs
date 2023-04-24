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

        private bool _emittersCreated;
        public bool IsConnected;
        public bool CheckConnection => SpeakerTransform != null && SpeakerTransform != SpeakerTarget;
        public bool InListenerRange;
        public MaterialColourModulator MaterialModulator = new();
        private Transform _headTransform;

        [Header("Speaker Attachment")]
        [Tooltip("Parent transform for speakers to target. Defaults to this frame's Actor's transform.")]
        public Transform SpeakerTarget;
        [Tooltip("Assigned to the speaker's transform when connected, otherwise the frame's Speaker Target.")]
        public Transform SpeakerTransform;
        public int SpeakerIndex;
        
        [NonReorderable]
        public List<StableEmitterAuth> StableEmitters = new();
        [NonReorderable]
        public List<VolatileEmitterAuth> VolatileEmitters = new();

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
            CreateEntity(EntityIndex);
            InitialiseEmitters();
            
            SynthManager.Instance.RegisterFrame(this);
        }

        private void OnEnable()
        {
            if (Actor != null)
                Actor.OnNewValidCollision += TriggerCollisionEmitters;

            if (SynthManager.Instance == null)
                return;

            SynthManager.Instance.RegisterFrame(this);
            RecreateEmitterEntities();
        }

        private void OnDisable()
        {
            if (Actor != null)
                Actor.OnNewValidCollision -= TriggerCollisionEmitters;
            BeforeDestroyingEntity();
            SynthManager.Instance.DeregisterFrame(this);
        }
        
        private void InitialiseEmitters()
        {
            for (var i = 0; i < StableEmitters.Count; i++)
                if (StableEmitters[i] != null)
                    StableEmitters[i].Initialise(i, name, in Actor);

            for (var i = 0; i < VolatileEmitters.Count; i++)
                if (VolatileEmitters[i] != null)
                    VolatileEmitters[i].Initialise(i, name, in Actor);
            
            _emittersCreated = true;
        }
        
        private void RecreateEmitterEntities()
        {
            if (!_emittersCreated)
                return;
            
            foreach (StableEmitterAuth emitter in StableEmitters)
                if (emitter != null)
                    emitter.InitialiseEntity(true);

            foreach (VolatileEmitterAuth emitter in VolatileEmitters)
                if (emitter != null)
                    emitter.InitialiseEntity(true);
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
            Manager.SetComponentData
            (ElementEntity, new FrameComponent {
                Index = EntityIndex, Position = SpeakerTarget.position
            });
        }

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
                Index = EntityIndex, Position = SpeakerTarget.position });
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

            if (SynthManager.Instance.GetSpeakerAtIndex(index, out SynthSpeaker speaker) != null)
            {
                SpeakerTransform = speaker.transform;
                SpeakerIndex = index;
                IsConnected = true;
            }
            else
            {
                Debug.Log(string.Format("{0} removing speaker connection component with index {1}.",
                    name, index.ToString()));
                RemoveConnectionComponent();
            }
        }

        private void RemoveConnectionComponent()
        {
            SpeakerTransform = SpeakerTarget;
            SpeakerIndex = -1;
            IsConnected = false;
            
            if (World.All.Count == 0 || !Manager.Exists(ElementEntity) || ElementEntity == Entity.Null)
                return;
            
            Manager.RemoveComponent<AloneOnSpeakerTag>(ElementEntity);
            Manager.RemoveComponent<SpeakerConnection>(ElementEntity);
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
            {
                if (emitter != null)
                    emitter.ApplyNewCollision(data);
            }
        }

        protected override void BeforeDestroyingEntity()
        {
            RemoveConnectionComponent();
            foreach (StableEmitterAuth emitter in StableEmitters)
                emitter.DestroyEntity();
            foreach (VolatileEmitterAuth emitter in VolatileEmitters)
                emitter.DestroyEntity();
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