using System;
using System.Collections.Generic;
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
    public class EmitterAuth : ISerializationCallbackReceiver
    {
        #region FIELDS & PROPERTIES

        public int EntityIndex { get; private set; }
        private EntityArchetype _elementArchetype;
        private EntityManager _manager;
        private Entity _emitterEntity;

        public PropagateCondition PlaybackCondition;
        [SerializeReference] public BaseEmitterObject EmitterAsset;

        [Range(0f, 2f)] public float EmitterVolume = 1;
        [Range(0f, 1f)] public float AgeFadeOut;

        public bool ReflectPlayheadAtLimit;
        public EmitterAttenuator DynamicAttenuation;
        public DSPClass[] DSPChainParams;

        
        [Header("Runtime Debug")] 
        [SerializeField] private bool ObjectConstructed;
        [SerializeField] private bool FrameInitialised;
        [SerializeField] private bool EntityInitialised;
        [SerializeField] private bool IsConnected;
        [SerializeField] private bool IsPlaying;

        
        private string _frameName;
        private ActorObject _actor;
        private CollisionData _collisionData;

        public bool IsVolatile => EmitterAsset is VolatileEmitter;

        #endregion

        #region CONSTRUCTOR / RESET METHOD

        public EmitterAuth() { }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (!ObjectConstructed) { Reset(); }
        }

        public void Reset()
        {
            if (ObjectConstructed)
            {
                Debug.LogWarning("EmitterAuth: Reset called on initialised EmitterAuth.");
                return;
            }
            PlaybackCondition = IsVolatile ? PropagateCondition.Collision : PropagateCondition.Constant;
            EmitterVolume = 1;
            AgeFadeOut = IsVolatile ? 1 : 0.95f;
            ReflectPlayheadAtLimit = !IsVolatile;
            DynamicAttenuation = new EmitterAttenuator();
            ObjectConstructed = true;
        }

        #endregion

        #region INITIALISATION METHODS

        public void Initialise(int index, string frameName, in ActorObject actor)
        {
            EntityIndex = index;
            _frameName = frameName;
            _actor = actor;

            if (FrameInitialised)
                return;

            if (EmitterAsset == null)
                throw new Exception("EmitterAsset is null.");

            if (EmitterAsset.AudioObject == null)
                throw new Exception("EmitterAsset.AudioAsset is null.");

            if (IsVolatile)
                PlaybackCondition = PropagateCondition.Collision;

            if (PlaybackCondition == PropagateCondition.Collision && !IsVolatile)
                throw new Exception("Only Volatile Emitters can be assigned with 'Collision' playback condition.");

            if (PlaybackCondition != PropagateCondition.Collision && IsVolatile)
                throw new Exception("Volatile Emitters can only be assigned with 'Collision' playback condition.");

            if (actor == null)
                throw new Exception("Actor is null.");

            EmitterAsset.InitialiseParameters(in actor);
            FrameInitialised = true;
            InitialiseEntity();
        }

        public void InitialiseEntity()
        {
            if (!FrameInitialised)
                throw new Exception("EmitterAuth: EmitterAuth not initialised.");

            _manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _elementArchetype = _manager.CreateArchetype(typeof(EmitterComponent));
            _emitterEntity = _manager.CreateEntity(_elementArchetype);

#if UNITY_EDITOR
            _manager.SetName
            (_emitterEntity,
                _frameName + "." + EntityIndex + "." + Enum.GetName(typeof(PropagateCondition), PlaybackCondition));
#endif
            EntityInitialised = true;
            InitialiseComponents();
        }

        public void InitialiseComponents()
        {
            if (!EntityInitialised)
                throw new Exception("EmitterAuth: Entity not initialised.");

            _manager.SetComponentData
            (_emitterEntity, new EmitterComponent {
                ReflectPlayhead = ReflectPlayheadAtLimit,
                AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                LastSampleIndex = -1,
                SamplesUntilFade = -1,
                SamplesUntilDeath = -1,
                LastGrainDuration = -1,
                EmitterVolume = EmitterVolume,
                DynamicAmplitude = 0,
                ModVolume = new ModulationComponent(),
                ModPlayhead = new ModulationComponent(),
                ModDuration = new ModulationComponent(),
                ModDensity = new ModulationComponent(),
                ModTranspose = new ModulationComponent(),
                ModLength = new ModulationComponent()
            });

            _manager.AddBuffer<AudioEffectParameters>(_emitterEntity);
            DynamicBuffer<AudioEffectParameters> dspParams = _manager.GetBuffer<AudioEffectParameters>(_emitterEntity);

            foreach (DSPClass t in DSPChainParams)
                dspParams.Add(t.GetDSPBufferElement());

            if (IsVolatile) _manager.AddComponentData(_emitterEntity, new EmitterVolatileTag());
        }

        #endregion

        #region STATE/UPDATE METHODS

        public void ApplyNewCollision(CollisionData collisionData)
        {
            _collisionData = collisionData;

            if (!IsVolatile || !EntityInitialised || !FrameInitialised)
                return;

            IsPlaying = true;
        }

        public void UpdateEmitterEntity(bool inListenerRange, bool isConnected, int speakerIndex)
        {
            if (!EntityInitialised || !FrameInitialised) { return; }

            IsConnected = isConnected;

            if (!IsConnected)
                DynamicAttenuation.Muted = true;

            if (!IsVolatile)
                IsPlaying = _actor.IsColliding || PlaybackCondition != PropagateCondition.Contact;

            if (IsPlaying && IsConnected && inListenerRange)
            {
                var data = _manager.GetComponentData<EmitterComponent>(_emitterEntity);
                UpdateEmitterComponent(ref data, speakerIndex);
                _manager.SetComponentData(_emitterEntity, data);
                _manager.AddComponent<EmitterReadyTag>(_emitterEntity);
            }
            else { _manager.RemoveComponent<EmitterReadyTag>(_emitterEntity); }

            if (IsVolatile)
                IsPlaying = false;
        }

        #endregion

        #region UPDATE EMITTER COMPONENT

        public void UpdateEmitterComponent(ref EmitterComponent data, int speakerIndex)
        {
            List<ModulationComponent> modulations = EmitterAsset.GetModulationComponents();

            if (modulations.Count != EmitterAsset.GetParameterCount)
                throw new Exception("EmitterAsset has incorrect number of modulations.");

            // data.LastSampleIndex = data.SpeakerIndex == speakerIndex ? data.LastSampleIndex : -1;
            // data.LastGrainDuration = data.SpeakerIndex == speakerIndex ? data.LastGrainDuration : -1;

            data = new EmitterComponent {
                SpeakerIndex = speakerIndex,
                AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                LastSampleIndex = IsVolatile ? -1 : data.LastSampleIndex,
                LastGrainDuration = IsVolatile ? -1 : data.LastGrainDuration,
                SamplesUntilFade = IsVolatile ? -1 : int.MaxValue,  //_actor.Life.SamplesUntilFade(AgeFadeOut),
                SamplesUntilDeath = IsVolatile ? -1 : int.MaxValue, //_actor.Life.SamplesUntilDeath(),
                ReflectPlayhead = ReflectPlayheadAtLimit,
                EmitterVolume = EmitterVolume,
                DynamicAmplitude = DynamicAttenuation.CalculateAmplitudeMultiplier(IsConnected, _actor),
                ModVolume = modulations[0],
                ModPlayhead = modulations[1],
                ModDuration = modulations[2],
                ModDensity = modulations[3],
                ModTranspose = modulations[4],
                ModLength = IsVolatile ? modulations[5] : new ModulationComponent()
            };

            UpdateDSPEffectsBuffer();
        }

        protected void UpdateDSPEffectsBuffer(bool clear = true)
        {
            //--- TODO not sure if clearing and adding again is the best way to do this.
            DynamicBuffer<AudioEffectParameters> dspBuffer = _manager.GetBuffer<AudioEffectParameters>(_emitterEntity);
            if (clear) dspBuffer.Clear();
            foreach (DSPClass t in DSPChainParams)
                dspBuffer.Add(t.GetDSPBufferElement());
        }

        #endregion

        public void OnDestroy() { DestroyEntity(); }

        private void DestroyEntity()
        {
            try { _manager.DestroyEntity(_emitterEntity); }
            catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) { }
        }
    }
}