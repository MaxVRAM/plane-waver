using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

using MaxVRAM.Extensions;
using Unity.Mathematics;

namespace PlaneWaver
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
        [Tooltip("Points to the speaker's transform when connected, otherwise the frame's Speaker Target.")]
        public Transform SpeakerTransform;
        public MaterialColourModulator MaterialModulator = new();
        
        [Header("Emitter Lists")]
        public List<BaseEmitterScriptable> ConstantEmitters = new();
        public List<BaseEmitterScriptable> ContactEmitters = new();
        public List<BaseEmitterScriptable> NonContactEmitters = new();
        public List<BaseEmitterScriptable> CollisionEmitters = new();
        
        private Transform _headTransform;
        private CollisionData _collisionData;
        
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
            Archetype = Manager.CreateArchetype(typeof(FrameComponent));
            InitialiseEntity();
            //SetIndex(GrainBrain.Instance.RegisterFrame(this));
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

        private void TriggerCollisionEmitters(CollisionData data)
        {
            foreach (BaseEmitterScriptable emitter in CollisionEmitters)
            {
                emitter.ArmCollisionEmitter(data);
            }
        }
        
        #region RUNTIME UPDATES
        
        private void Update()
        {
        }
        
        #endregion
    }

    #region FRAME COMPONENTS
    
    public struct FrameComponent : IComponentData
    {
        public int Index;
        public float3 Position;
    }

    public struct ConnectedToSpeaker : IComponentData
    {
        public int SpeakerIndex;
    }
    
    public struct InListenerRange : IComponentData { }
    
    #endregion
}

