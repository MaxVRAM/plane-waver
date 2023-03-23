﻿using System;
using PlaneWaver.DSP;
using PlaneWaver.Interaction;
using PlaneWaver.Modulation;
using Unity.Entities;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public abstract class BaseEmitterAuth : ISerializationCallbackReceiver
    {
        #region FIELDS & PROPERTIES

        // Runtime fields/properties
        public int EntityIndex { get; private set; }
        protected string FrameName;
        protected EntityArchetype ElementArchetype;
        protected EntityManager Manager;
        protected Entity EmitterEntity;
        protected ActorObject Actor;
        protected CollisionData CollisionData;
        public bool IsVolatile => this is VolatileEmitterAuth;

        public PlaybackCondition Condition;
        protected BaseEmitterObject EmitterAsset;
        public bool ReflectPlayhead;
        [Range(0f, 2f)] public float VolumeAdjustment = 1;
        // This is temporary until I implement age fade in/out for non-volatile emitters in the DynamicAttenuator.
        [Range(0f, 1f)] public float AgeFadeOut;
        public EmitterAttenuator DynamicAttenuation;
        [HideInInspector] public DSPClass[] DSPChainParams;
        public EmitterAuthRuntimeStates RuntimeState = new();
        
        #endregion

        #region MANUAL SERLIALISATION CONSTRUCTOR / RESET
        
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (!RuntimeState.ObjectConstructed) 
                Reset();
        }

        public virtual void Reset()
        {
            DSPChainParams = Array.Empty<DSPClass>();
            RuntimeState ??= new EmitterAuthRuntimeStates();
            RuntimeState.ObjectConstructed = true;
            DynamicAttenuation = new EmitterAttenuator();
            VolumeAdjustment = 1;
        }

        #endregion

        #region INITIALISATION METHODS

        public bool Initialise(int index, string frameName, in ActorObject actor)
        {
            EntityIndex = index;
            FrameName = frameName;
            Actor = actor;
            
            if (RuntimeState.BaseInitialised)
                return true;

            if (!InitialiseSubType())
            {
                Debug.LogWarning("EmitterAuth: SubType not initialised.");
                return false;
            }

            if (EmitterAsset == null)
            {
                Debug.LogWarning("EmitterAsset is null.");
                return false;
            }

            if (EmitterAsset.AudioObject == null)
            {
                Debug.LogWarning("EmitterAsset.AudioAsset is null.");
                return false;
            }
            
            if (actor == null)
                Debug.LogWarning("Actor is null.");

            EmitterAsset.InitialiseParameters(in actor);
            RuntimeState.BaseInitialised = true;
            InitialiseEntity();
            return RuntimeState.EntityInitialised;
        }

        public virtual bool InitialiseSubType() { return false; }

        public bool InitialiseEntity(bool ignoreBase = false)
        {
            if (!ignoreBase && !RuntimeState.BaseInitialised)
            {
                Debug.LogWarning("EmitterAuth: Not initialised. Cannot create entity.");
                return false;
            }

            if (RuntimeState.EntityInitialised)
                return true;
            
            Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            ElementArchetype = Manager.CreateArchetype(typeof(EmitterComponent));
            EmitterEntity = Manager.CreateEntity(ElementArchetype);

#if UNITY_EDITOR
            Manager.SetName(EmitterEntity, FrameName + "." + (IsVolatile ? "Volatile" : "Stable"));
#endif
            
            RuntimeState.EntityInitialised = InitialiseComponents();
            return true;
        }

        public bool InitialiseComponents()
        {
            if (!Manager.Exists(EmitterEntity))
            {
                Debug.LogWarning("EmitterAuth: Entity not initialised.");
                return false;                
            }

            Manager.SetComponentData
            (EmitterEntity, new EmitterComponent {
                ReflectPlayhead = ReflectPlayhead,
                AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                LastSampleIndex = -1,
                SamplesUntilFade = -1,
                SamplesUntilDeath = -1,
                LastGrainDuration = -1,
                EmitterVolume = VolumeAdjustment,
                DynamicAmplitude = 0,
                ModVolume = new ModulationComponent(),
                ModPlayhead = new ModulationComponent(),
                ModDuration = new ModulationComponent(),
                ModDensity = new ModulationComponent(),
                ModTranspose = new ModulationComponent(),
                ModLength = new ModulationComponent()
            });

            if (IsVolatile)
                Manager.AddComponentData(EmitterEntity, new EmitterVolatileTag());
            
            Manager.AddBuffer<AudioEffectParameters>(EmitterEntity);
            DynamicBuffer<AudioEffectParameters> dspParams = Manager.GetBuffer<AudioEffectParameters>(EmitterEntity);

            if (DSPChainParams == null)
                return true;
            foreach (DSPClass t in DSPChainParams)
                dspParams.Add(t.GetDSPBufferElement());
            
            return true;
        }

        #endregion

        #region STATE/UPDATE METHODS

        public virtual void ApplyNewCollision(CollisionData collisionData)
        {
            CollisionData = collisionData;
        }
    
        public void UpdateEmitterEntity(bool inListenerRange, bool isConnected, int speakerIndex)
        {
            if (!RuntimeState.IsInitialised()) 
                return;

            RuntimeState.SetConnected(isConnected && inListenerRange);
            
            if (!RuntimeState.IsReady())
            {
                DynamicAttenuation.UpdateConnectionState(false);
                Manager.RemoveComponent<EmitterReadyTag>(EmitterEntity);
                return;
            }
            
            DynamicAttenuation.UpdateConnectionState(true);

            if (!IsPlaying())
                return;
            
            var data = Manager.GetComponentData<EmitterComponent>(EmitterEntity);
            data = UpdateEmitterComponent(data, speakerIndex);
            Manager.SetComponentData(EmitterEntity, data);
            Manager.AddComponent<EmitterReadyTag>(EmitterEntity);
            
            if (IsVolatile)
                RuntimeState.SetPlaying(false);
        }
        
        public virtual bool IsPlaying()
        {
            return RuntimeState.IsPlaying;
        }

        #endregion

        #region UPDATE EMITTER COMPONENT

        public virtual EmitterComponent UpdateEmitterComponent(EmitterComponent previousData, int speakerIndex)
        {
            return previousData;
        }
        
        protected void UpdateDSPEffectsBuffer(bool clear = true)
        {
            //--- TODO not sure if clearing and adding again is the best way to do this.
            if (!RuntimeState.EntityInitialised || Manager.Exists(EmitterEntity))
                return;
            DynamicBuffer<AudioEffectParameters> dspBuffer = Manager.GetBuffer<AudioEffectParameters>(EmitterEntity);
            if (clear)
                dspBuffer.Clear();
            if (DSPChainParams == null)
                return;
            foreach (DSPClass t in DSPChainParams)
                dspBuffer.Add(t.GetDSPBufferElement());
        }

        #endregion

        public void OnDestroy()
        {
            DestroyEntity();
        }

        public void DestroyEntity()
        {
            RuntimeState.EntityInitialised = false;
            try { Manager.DestroyEntity(EmitterEntity); }
            catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) { }
        }
    }
}