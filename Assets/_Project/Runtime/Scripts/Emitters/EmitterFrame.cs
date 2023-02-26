using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

using MaxVRAM.Extensions;
using Unity.Mathematics;

namespace PlaneWaver
{
    public class EmitterFrame : SynthEntity
    {
        #region FIELDS & PROPERTIES

        [Header("Actor")]
        [Tooltip("The actor component that this emitter frame is associated with.")]
        public Actor ActorObject;
        
        [Header("Speaker Attachment")]
        [Tooltip("Parent transform position for speakers to target for this host. Creates a new child transform if not provided.")]
        public Transform SpeakerTarget;
        [Tooltip("Sets to the connected speaker's transform. Sets to this host's Speaker Target if no speaker is connected.")]
        public Transform SpeakerTransform;
        public MaterialColourModulator MaterialModulator = new();
        private Transform _headTransform;
        
        #endregion
        
        
        void Start()
        {
            InitialiseAttachmentPoint();
            if (MaterialModulator._Renderer == null && ActorObject != null)
                MaterialModulator._Renderer = ActorObject.GetComponentInChildren<Renderer>();
            _headTransform = FindObjectOfType<Camera>().transform;
            
            _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _Archetype = _EntityManager.CreateArchetype(typeof(HostComponent));
            SetIndex(GrainBrain.Instance.RegisterFrame(this));
        }
        
        private void InitialiseAttachmentPoint()
        {
            if (SpeakerTarget == null)
                SpeakerTarget = new GameObject("SpeakerTarget")
                                .SetParentAndZero(ActorObject.transform).transform;
            SpeakerTransform = SpeakerTarget;
        }
    }
    
    public override void SetEntityType()
    {
        _EntityType = SynthEntityType.Frame;
    }
    
    public struct FrameComponent : IComponentData
    {
        public int Index;
        public bool InListenerRange;
        public float3 Position;
    }
    
    public struct ConnectedToSpeaker : IComponentData
    {
        public int SpeakerIndex;
    }
    
    
}

