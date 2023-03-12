using System;
using PlaneWaver.DSP;
using PlaneWaver.Interaction;
using PlaneWaver.Modulation;
using Unity.Entities;
using UnityEngine;

// Trying to call the constructor on the hosted class, but it's not working.
// Attempted calling it from a custom Inspector, but the data wouldn't serialise when referencing
// the Serializable class. So I'm trying to call it from the constructor of the hosting class.
// More information is here:
// https://forum.unity.com/threads/how-to-inherit-from-list-t-make-a-list-t-propertydrawer.543154/
// https://docs.unity3d.com/ScriptReference/ISerializationCallbackReceiver.html
// ISerializationCallbackReceiver


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

        public EmitterAuthRuntimeStates RuntimeState = new();

        // Unsure if I need to serialise the reference of this class, or if it's enough to serialise the data
        [HideInInspector] [SerializeReference]
        protected BaseEmitterObject EmitterAsset;
        
        //private BaseEmitterObject _emitterObject;
        
        public PlaybackCondition Condition;
        [Range(0f, 2f)] public float VolumeAdjustment = 1;
        // This is temporary until I implement age fade in/out for non-volatile emitters in the DynamicAttenuator.
        [Range(0f, 1f)] public float AgeFadeOut;

        public bool ReflectPlayheadAtLimit;
        public EmitterAttenuator DynamicAttenuation;
        
        // Making this private for now. It's not used in the current implementation.
        protected DSPClass[] DSPChainParams;
        

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
            Debug.Log( "BaseEmitterAuth: Reset");
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
            return true;
        }

        public virtual bool InitialiseSubType() { return false; }

        public void InitialiseEntity()
        {
            if (!RuntimeState.BaseInitialised)
                Debug.LogWarning("EmitterAuth: Not initialised. Cannot create entity.");

            Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            ElementArchetype = Manager.CreateArchetype(typeof(EmitterComponent));
            EmitterEntity = Manager.CreateEntity(ElementArchetype);

#if UNITY_EDITOR
            Manager.SetName
            (EmitterEntity,
                FrameName + "." + EntityIndex + "." + (IsVolatile ? "Volatile" : "Stable"));
#endif
            
            RuntimeState.EntityInitialised = true;
            InitialiseComponents();
        }

        public void InitialiseComponents()
        {
            if (!RuntimeState.EntityInitialised)
                Debug.LogWarning("EmitterAuth: Entity not initialised.");

            Manager.SetComponentData
            (EmitterEntity, new EmitterComponent {
                ReflectPlayhead = ReflectPlayheadAtLimit,
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

            if (IsVolatile) Manager.AddComponentData(EmitterEntity, new EmitterVolatileTag());
            
            Manager.AddBuffer<AudioEffectParameters>(EmitterEntity);
            DynamicBuffer<AudioEffectParameters> dspParams = Manager.GetBuffer<AudioEffectParameters>(EmitterEntity);

            if (DSPChainParams == null) return;
            foreach (DSPClass t in DSPChainParams)
                dspParams.Add(t.GetDSPBufferElement());
        }

        #endregion

        #region STATE/UPDATE METHODS

        public virtual void ApplyNewCollision(CollisionData collisionData)
        {
            CollisionData = collisionData;
        }
    
        public void UpdateEmitterEntity(bool inListenerRange, bool isConnected, int speakerIndex)
        {
            if (!RuntimeState.IsInitialised()) { return; }

            if (!RuntimeState.SetConnected(isConnected && inListenerRange))
            {
                DynamicAttenuation.UpdateConnectionState(false);
                Manager.RemoveComponent<EmitterReadyTag>(EmitterEntity);
                return;
            }
            
            DynamicAttenuation.UpdateConnectionState(true);

            if (!RequestPlayback()) return;

            var data = Manager.GetComponentData<EmitterComponent>(EmitterEntity);
            data = UpdateEmitterComponent(data, speakerIndex);
            Manager.SetComponentData(EmitterEntity, data);
            Manager.AddComponent<EmitterReadyTag>(EmitterEntity);
        }
        
        public virtual bool RequestPlayback()
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
            DynamicBuffer<AudioEffectParameters> dspBuffer = Manager.GetBuffer<AudioEffectParameters>(EmitterEntity);
            if (clear) dspBuffer.Clear();
            if (DSPChainParams == null) return;
            foreach (DSPClass t in DSPChainParams)
                dspBuffer.Add(t.GetDSPBufferElement());
        }

        #endregion

        public void OnDestroy() { DestroyEntity(); }

        private void DestroyEntity()
        {
            try { Manager.DestroyEntity(EmitterEntity); }
            catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) { }
        }
    }
}