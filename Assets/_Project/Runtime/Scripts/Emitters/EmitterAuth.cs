using System;
using System.Collections.Generic;
using PlaneWaver.DSP;
using PlaneWaver.Interaction;
using PlaneWaver.Modulation;
using Unity.Entities;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public class EmitterAuth
    {
        #region FIELDS & PROPERTIES

        public enum PropagateCondition
        {
            Constant, Contact, Airborne, Volatile
        }

        public int EntityIndex { get; private set; }
        //private SynthElementType _elementType = SynthElementType.Emitter;
        private EntityArchetype _elementArchetype;
        private EntityManager _manager;
        private Entity _emitterEntity;

        public PropagateCondition PlaybackCondition;

        public BaseEmitterObject EmitterAsset;

        [Range(0f, 2f)] public float EmitterVolume = 1;
        [Range(0f, 1f)] public float AgeFadeOut;

        public bool ReflectPlayheadAtLimit;
        public EmitterAttenuator DynamicAttenuation;
        public DSPClass[] DSPChainParams;

        [Header("Runtime Debug")] [SerializeField]
        private bool FrameInitialised;
        [SerializeField] private bool EntityInitialised;
        [SerializeField] private bool IsConnected;
        [SerializeField] private bool IsPlaying;

        private string _frameName;
        private ActorObject _actor;
        private CollisionData _collisionData;

        public bool IsVolatile => EmitterAsset is VolatileEmitterObject;

        #endregion

        #region CONSTRUCTOR / RESET METHOD

        public EmitterAuth()
        {
            PlaybackCondition = PropagateCondition.Constant;
            AgeFadeOut = 0.95f;
            ReflectPlayheadAtLimit = true;
            DynamicAttenuation = new EmitterAttenuator();
        }

        public void Reset()
        {
            if (EmitterAsset == null)
                return;

            if (IsVolatile)
                PlaybackCondition = PropagateCondition.Volatile;

            EmitterVolume = 1;
            AgeFadeOut = IsVolatile ? 1 : 0.95f;
            ReflectPlayheadAtLimit = !IsVolatile;
            DynamicAttenuation = new EmitterAttenuator();
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
                PlaybackCondition = PropagateCondition.Volatile;

            if (PlaybackCondition == PropagateCondition.Volatile && !IsVolatile)
                throw new Exception("PlaybackType is Volatile but EmitterAsset is not Volatile.");

            if (PlaybackCondition != PropagateCondition.Volatile && IsVolatile)
                throw new Exception("PlaybackType is not Volatile but EmitterAsset is Volatile.");

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

            for (var i = 0; i < DSPChainParams.Length; i++)
                dspParams.Add(DSPChainParams[i].GetDSPBufferElement());

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
            for (var i = 0; i < DSPChainParams.Length; i++)
                dspBuffer.Add(DSPChainParams[i].GetDSPBufferElement());
        }

        #endregion

        public void OnDestroy() { DestroyEntity(); }

        private void DestroyEntity()
        {
            try { _manager.DestroyEntity(_emitterEntity); }
            catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) { }
        }
    }

    public struct EmitterComponent : IComponentData
    {
        public int SpeakerIndex;
        public int AudioClipIndex;
        public int LastSampleIndex;
        public int LastGrainDuration;
        public int SamplesUntilFade;
        public int SamplesUntilDeath;
        public bool ReflectPlayhead;
        public float EmitterVolume;
        public float DynamicAmplitude;
        public ModulationComponent ModVolume;
        public ModulationComponent ModPlayhead;
        public ModulationComponent ModDuration;
        public ModulationComponent ModDensity;
        public ModulationComponent ModTranspose;
        public ModulationComponent ModLength;
    }

    public struct EmitterReadyTag : IComponentData { }

    public struct EmitterVolatileTag : IComponentData { }
}