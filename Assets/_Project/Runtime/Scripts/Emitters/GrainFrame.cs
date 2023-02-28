using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

using MaxVRAM.Extensions;

using PlaneWaver.DSP;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    public class GrainFrame : SynthElement
    {
        #region CLASS DEFINITIONS

        [Header("Actor")]
        [Tooltip("The Actor component that provides this emitter frame with modulation values.")]
        public Actor ActorObject;
        
        [Header("Speaker Attachment")]
        [Tooltip("Parent transform for speakers to target. Defaults to this frame's Actor's transform.")]
        public Transform SpeakerTarget;
        [Tooltip("Assigned to the speaker's transform when connected, otherwise the frame's Speaker Target.")]
        public Transform SpeakerTransform;
        public int SpeakerIndex;
        public bool IsConnected => SpeakerTransform != SpeakerTarget;
        public bool InListenerRange { get; private set; }
        public MaterialColourModulator MaterialModulator = new();
        
        [Header("Emitters")]
        public List<EmitterAuth> StableEmitters = new();
        public List<EmitterAuth> VolatileEmitters = new();
        
        private Transform _headTransform;
        
        #endregion

        #region INITIALISATION METHODS

        private void Start()
        {
            ActorObject = GetComponent<Actor>() ?? gameObject.AddComponent<Actor>();
            ActorObject.OnNewValidCollision += TriggerCollisionEmitters;
            
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
                StableEmitters[i].Initialise(name, i, ActorObject);
            
            for (var i = 0; i < VolatileEmitters.Count; i++)
                VolatileEmitters[i].Initialise(name, i, ActorObject);
        }

        private void InitialiseAttachmentPoint()
        {
            if (SpeakerTarget == null)
                SpeakerTarget = new GameObject("SpeakerTarget")
                                .SetParentAndZero(ActorObject.transform).transform;
            SpeakerTransform = SpeakerTarget;
        }
        
        private void InitialiseMaterialModulator()
        {
            if (MaterialModulator._Renderer == null && ActorObject != null)
                MaterialModulator._Renderer = ActorObject.GetComponentInChildren<Renderer>();
        }
        
        protected override void InitialiseComponents()
        {
            Manager.SetComponentData(ElementEntity, new FrameComponent
            {
                    Index = EntityIndex,
                    Position = SpeakerTarget.position,
            });
        }
        
        private void OnDisable()
        {
            ActorObject.OnNewValidCollision -= TriggerCollisionEmitters;
        }

        #endregion
        
        #region RUNTIME UPDATES
        
        protected override void ProcessComponents()
        {
            UpdatePosition();
            GetComponentUpdates();
            UpdateEmitters();
        }
        
        private void UpdatePosition()
        {
            Manager.SetComponentData(ElementEntity, new FrameComponent
            {
                    Index = EntityIndex,
                    Position = SpeakerTarget.position,
            });
        }

        private void GetComponentUpdates()
        {
            InListenerRange = Manager.HasComponent(ElementEntity, typeof(InListenerRangeTag));
            SpeakerIndex = Manager.GetComponentData<SpeakerIndex>(ElementEntity).Value;
            
            SpeakerTransform = GrainBrain.Instance.IsSpeakerAtIndex(SpeakerIndex, out SpeakerAuthoring speaker)
                    ? speaker.transform
                    : SpeakerTarget;
        }
        
        private void UpdateEmitters()
        {
            foreach (EmitterAuth emitter in StableEmitters)
                emitter.UpdateEmitterEntity(IsConnected, SpeakerIndex);
            
            foreach (EmitterAuth emitter in VolatileEmitters)
                emitter.UpdateEmitterEntity(IsConnected, SpeakerIndex);
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
    
    public struct InListenerRangeTag : IComponentData { }
    
    #endregion
}

