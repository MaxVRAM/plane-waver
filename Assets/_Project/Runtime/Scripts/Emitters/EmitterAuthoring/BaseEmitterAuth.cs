using System;
using PlaneWaver.DSP;
using PlaneWaver.Interaction;
using PlaneWaver.Modulation;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public abstract class BaseEmitterAuth : ISerializationCallbackReceiver
    {
        #region FIELDS & PROPERTIES

        public int EntityIndex { get; private set; }
        protected string FrameName;
        protected EntityArchetype ElementArchetype;
        protected EntityManager Manager;
        protected Entity EmitterEntity;
        protected ActorObject Actor;
        protected CollisionData CollisionData;
        public bool IsVolatile => this is VolatileEmitterAuth;

        public bool Enabled;
        public PlaybackCondition Condition;
        protected BaseEmitterObject EmitterAsset;
        public bool ReflectPlayhead;
        public EmitterAttenuator Attenuation;
        public EmitterRuntimeStates RuntimeState = new();
        public ParameterInstance[] Parameters;

        [HideInInspector] public DSPClass[] DSPChainParams;

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
            RuntimeState ??= new EmitterRuntimeStates();
            RuntimeState.ObjectConstructed = true;
            Attenuation = new EmitterAttenuator();
            Enabled = true;
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

            Parameters = new ParameterInstance[EmitterAsset.Parameters.Count];

            for (var i = 0; i < Parameters.Length; i++)
                Parameters[i] = new ParameterInstance(EmitterAsset.Parameters[i]);

            RuntimeState.BaseInitialised = true;
            Enabled = true;

            InitialiseEntity();

            return RuntimeState.EntityInitialised;
        }

        public virtual bool InitialiseSubType()
        {
            return false;
        }

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
            Manager.SetName
            (EmitterEntity, FrameName +
                            "." +
                            (IsVolatile
                                    ? "Volatile"
                                    : "Stable"));
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
                Gain = 1,
                ModVolume = new ParameterComponent(),
                ModPlayhead = new ParameterComponent(),
                ModDuration = new ParameterComponent(),
                ModDensity = new ParameterComponent(),
                ModTranspose = new ParameterComponent(),
                ModLength = new ParameterComponent()
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

        public virtual bool IsPlaying()
        {
            return true;
        }

        public void UpdateEmitterEntity(bool inListenerRange, bool isConnected, int speakerIndex)
        {
            if (!RuntimeState.IsInitialised())
                return;

            RuntimeState.SetConnected(isConnected && inListenerRange);

            if (!RuntimeState.IsReady())
            {
                Attenuation.UpdateConnectionState(false);
                Manager.RemoveComponent<EmitterReadyTag>(EmitterEntity);
                return;
            }

            Attenuation.UpdateConnectionState(true);

            if (!IsPlaying())
                return;

            var emitterComponent = Manager.GetComponentData<EmitterComponent>(EmitterEntity);

            foreach (ParameterInstance param in Parameters) { param.UpdateInputValue(Actor); }

            emitterComponent = UpdateEmitterComponent(emitterComponent, speakerIndex);
            Manager.SetComponentData(EmitterEntity, emitterComponent);
            Manager.AddComponent<EmitterReadyTag>(EmitterEntity);

            if (IsVolatile)
                RuntimeState.SetPlaying(false);
        }

        public virtual EmitterComponent UpdateEmitterComponent(EmitterComponent emitter, int speakerIndex)
        {
            return emitter;
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