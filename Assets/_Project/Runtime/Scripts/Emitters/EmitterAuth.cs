using System;
using System.Collections.Generic;
using NaughtyAttributes;
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
        private SynthElementType _elementType = SynthElementType.Emitter;
        private EntityArchetype _elementArchetype;
        private EntityManager _manager;
        private Entity _emitterEntity;
        private bool _entityInitialised;

        [AllowNesting] [DisableIf("IsVolatile")]
        public PropagateCondition PlaybackCondition;
        [Expandable] public EmitterObject EmitterAsset;
        [Range(0f, 2f)] public float EmitterVolume = 1;
        [AllowNesting] [DisableIf("IsVolatile")] [Range(0f, 1f)]
        public float AgeFadeOut = 0.95f;
        public EmitterAttenuator DynamicAttenuation;
        public bool ReflectPlayheadAtLimit;

        [Header("Runtime Data")] [SerializeField]
        private bool Initialised;
        [SerializeField] private bool IsPlaying;
        [SerializeField] private bool IsConnected;

        private string _frameName;
        private Actor _actor;
        private CollisionData _collisionData;

        public bool IsVolatile => EmitterAsset is VolatileEmitterObject;

        #endregion

        #region RESET METHOD

        [Button("Reset")]
        public void Reset()
        {
            if (EmitterAsset == null)
                return;

            if (IsVolatile)
                PlaybackCondition = PropagateCondition.Volatile;

            EmitterVolume = 1;
            DynamicAttenuation = new EmitterAttenuator();
            ReflectPlayheadAtLimit = !IsVolatile;
        }

        #endregion

        #region INITIALISATION METHODS

        public void Initialise(int index, string frameName, in Actor actor)
        {
            EntityIndex = index;
            _frameName = frameName;
            _actor = actor;

            if (Initialised)
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
            Initialised = true;
        }

        public void CreateEntity()
        {
            if (!Initialised)
                throw new Exception("EmitterAuth: EmitterAuth not initialised.");

            _manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _elementArchetype = _manager.CreateArchetype(typeof(EmitterComponent));
            _emitterEntity = _manager.CreateEntity(_elementArchetype);

#if UNITY_EDITOR
            _manager.SetName(
                _emitterEntity,
                _frameName + "." + EntityIndex + "." + Enum.GetName(typeof(PropagateCondition), PlaybackCondition)
            );
#endif
            _entityInitialised = true;
        }

        public void InitialiseComponents()
        {
            if (!_entityInitialised)
                throw new Exception("EmitterAuth: Entity not initialised.");

            _manager.SetComponentData(
                _emitterEntity,
                new EmitterComponent
                {
                    ReflectPlayhead = ReflectPlayheadAtLimit,
                    AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                    LastSampleIndex = -1,
                    SamplesUntilFade = -1,
                    SamplesUntilDeath = -1,
                    LastGrainDuration = -1,
                    EmitterVolume = EmitterVolume,
                    DynamicAmplitude = 0,
                    ParamVolume = new ModulationComponent(),
                    ParamPlayhead = new ModulationComponent(),
                    ParamDuration = new ModulationComponent(),
                    ParamDensity = new ModulationComponent(),
                    ParamTranspose = new ModulationComponent(),
                    ParamLength = new ModulationComponent()
                }
            );

            if (IsVolatile) _manager.AddComponentData(_emitterEntity, new EmitterVolatileTag());
        }

        #endregion

        #region STATE/UPDATE METHODS

        public void ApplyNewCollision(CollisionData collisionData)
        {
            _collisionData = collisionData;

            if (!IsVolatile ||
                !Initialised)
                return;

            IsPlaying = true;
        }

        public void UpdateEmitterEntity(bool inListenerRange, bool isConnected, int speakerIndex)
        {
            if (!_entityInitialised)
                return;

            IsConnected = isConnected;
            
            if (!IsConnected)
                DynamicAttenuation.Muted = true;

            if (!IsVolatile)
                IsPlaying = _actor.IsColliding || PlaybackCondition != PropagateCondition.Contact;
            else if (!IsConnected ||
                     !inListenerRange)
                IsPlaying = false;

            if (IsPlaying &&
                IsConnected &&
                inListenerRange)
            {
                var data = _manager.GetComponentData<EmitterComponent>(_emitterEntity);
                UpdateEmitterComponent(ref data, speakerIndex);
                _manager.SetComponentData(_emitterEntity, data);
                _manager.AddComponent<EmitterPlaybackReadyTag>(_emitterEntity);
            }
            else { _manager.RemoveComponent<EmitterPlaybackReadyTag>(_emitterEntity); }

            if (IsVolatile) IsPlaying = false;
        }

        #endregion

        #region UPDATE EMITTER COMPONENT

        public void UpdateEmitterComponent(ref EmitterComponent data, int speakerIndex)
        {
            List<ModulationComponent> modulations = EmitterAsset.GetModulationComponents();
            
            switch (IsVolatile)
            {
                case true when modulations.Count != 5:
                    throw new Exception("Volatile Emitter must have 5 modulation sources.");

                case false when modulations.Count != 4:
                    throw new Exception("Stable Emitter must have 4 modulation sources.");
            }

            data.LastSampleIndex = data.SpeakerIndex == speakerIndex ? data.LastSampleIndex : -1;
            data.LastGrainDuration = data.SpeakerIndex == speakerIndex ? data.LastGrainDuration : -1;

            data = new EmitterComponent
            {
                SpeakerIndex = speakerIndex,
                AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                LastSampleIndex = IsVolatile ? -1 : data.LastSampleIndex,
                LastGrainDuration = IsVolatile ? -1 : data.LastGrainDuration,
                SamplesUntilFade = IsVolatile ? -1 : _actor.Life.SamplesUntilFade(AgeFadeOut),
                SamplesUntilDeath = IsVolatile ? -1 : _actor.Life.SamplesUntilDeath(),
                ReflectPlayhead = ReflectPlayheadAtLimit,
                EmitterVolume = EmitterVolume,
                DynamicAmplitude = DynamicAttenuation.CalculateAmplitudeMultiplier(IsConnected, _actor),
                ParamVolume = modulations[0],
                ParamPlayhead = modulations[1],
                ParamDuration = modulations[2],
                ParamDensity = modulations[3],
                ParamTranspose = modulations[4],
                ParamLength = IsVolatile ? modulations[5] : new ModulationComponent()
            };
        }

        #endregion
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
        public ModulationComponent ParamVolume;
        public ModulationComponent ParamPlayhead;
        public ModulationComponent ParamDuration;
        public ModulationComponent ParamDensity;
        public ModulationComponent ParamTranspose;
        public ModulationComponent ParamLength;
    }

    public struct EmitterPlaybackReadyTag : IComponentData
    {
    }

    public struct EmitterVolatileTag : IComponentData
    {
    }
}