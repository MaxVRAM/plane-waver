using System;

using UnityEngine;
using Unity.Entities;

using NaughtyAttributes;

using PlaneWaver.Modulation;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    [Serializable]
    public class EmitterAuth
    {
        #region FIELDS & PROPERTIES
        
        public int EntityIndex { get; private set; }
        private SynthElementType _elementType = SynthElementType.Emitter;
        private EntityManager _manager;
        private EntityArchetype _elementArchetype;
        private Entity _emitterEntity;
        private bool _entityInitialised;
        
        [SerializeField] private bool Initialised;
        [SerializeField] private bool IsPlaying;
        [SerializeField] private bool IsConnected;
        [AllowNesting]
        [DisableIf("IsVolatile")]
        public PropagateCondition PlaybackCondition;
        [Range(0f, 2f)] public float EmitterVolume = 1;
        public Attenuator DynamicAttenuation;
        public bool ReflectPlayheadAtLimit;
        [Expandable]
        public EmitterObject EmitterAsset;

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
            DynamicAttenuation = new Attenuator();
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

            if (PlaybackCondition == PropagateCondition.Volatile && !IsVolatile)
                throw new Exception("PlaybackType is Volatile but EmitterAsset is not Volatile.");

            if (PlaybackCondition != PropagateCondition.Volatile && IsVolatile)
                throw new Exception("PlaybackType is not Volatile but EmitterAsset is Volatile.");
            
            EmitterAsset.InitialiseParameters();
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
            
            _manager.SetComponentData(_emitterEntity, new EmitterComponent
            {
                ReflectPlayhead = ReflectPlayheadAtLimit,
                AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                LastSampleIndex = -1,
                SamplesUntilFade = -1,
                SamplesUntilDeath = -1,
                LastGrainDuration = -1,
                EmitterVolume = EmitterVolume,
                DynamicAmplitude = 0,
                ParamVolume = new ModComponent(),
                ParamPlayhead = new ModComponent(),
                ParamDuration = new ModComponent(),
                ParamDensity = new ModComponent(),
                ParamTranspose = new ModComponent(),
                ParamLength = new ModComponent() 
            });
            
            if (IsVolatile) { _manager.AddComponentData(_emitterEntity, new EmitterVolatileTag()); }
        }
        
        #endregion
        
        #region STATE/UPDATE METHODS
        
        public void ApplyNewCollision(CollisionData collisionData)
        {
            _collisionData = collisionData;
            
            if (!IsVolatile || !Initialised)
                return;

            IsPlaying = true;
        }
        
        public void UpdateEmitterEntity(bool isConnected, int speakerIndex, bool inRange)
        {
            if (!_entityInitialised) 
                return;
            
            if (!isConnected && IsPlaying) 
                DynamicAttenuation.CalculateMuting(false);
            
            if (!IsVolatile)
                IsPlaying = _actor.IsColliding || PlaybackCondition != PropagateCondition.Contact;
            else if (!isConnected || !inRange)
                IsPlaying = false;
            
            if (IsPlaying && isConnected && inRange)
            {            
                var data = _manager.GetComponentData<EmitterComponent>(_emitterEntity);
                UpdateEmitterComponent(ref data, speakerIndex);
                _manager.SetComponentData(_emitterEntity, data);
                _manager.AddComponent<EmitterPlaybackReadyTag>(_emitterEntity);
            }
            else
            {
                _manager.RemoveComponent<EmitterPlaybackReadyTag>(_emitterEntity);
            }
            
            IsConnected = isConnected;
            if (IsVolatile) IsPlaying = false;
        }
        
        #endregion

        #region UPDATE EMITTER COMPONENT
        
        public void UpdateEmitterComponent(ref EmitterComponent data, int speakerIndex)
        {
            ModComponent[] modulations = EmitterAsset.UpdateModulations(_actor);
            data.LastSampleIndex = data.SpeakerIndex == speakerIndex ? data.LastSampleIndex : -1;
            data.LastGrainDuration = data.SpeakerIndex == speakerIndex ? data.LastGrainDuration : -1;
            data = new EmitterComponent
            {
                SpeakerIndex = speakerIndex,
                AudioClipIndex = EmitterAsset.AudioObject.ClipEntityIndex,
                LastSampleIndex = data.LastSampleIndex,
                LastGrainDuration = data.LastGrainDuration,
                SamplesUntilFade = _actor.ActorLifeController.SamplesUntilFade(EmitterAsset.AgeFadeOut),
                SamplesUntilDeath = _actor.ActorLifeController.SamplesUntilDeath(),
                ReflectPlayhead = ReflectPlayheadAtLimit,
                EmitterVolume = EmitterVolume,
                DynamicAmplitude = DynamicAttenuation.CalculateAmplitudeMultiplier(IsConnected, _actor),
                ParamVolume = modulations[0],
                ParamPlayhead = modulations[1],
                ParamDuration = modulations[2],
                ParamDensity = modulations[3],
                ParamTranspose = modulations[4],
                ParamLength = IsVolatile ? modulations[5] : new ModComponent()
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
        public ModComponent ParamVolume;
        public ModComponent ParamPlayhead;
        public ModComponent ParamDuration;
        public ModComponent ParamDensity;
        public ModComponent ParamTranspose;
        public ModComponent ParamLength;
    }
    
    public struct EmitterPlaybackReadyTag : IComponentData { }
    public struct EmitterVolatileTag : IComponentData { }
}