using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Unity.Entities;

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
        
        public PropagateCondition PlaybackCondition;
        public EmitterObject EmitterAsset;
        [Range(0f, 2f)] public float EmitterVolume = 1;
        public DynamicAmplitude Amplitude;
        public bool ReflectPlayhead;

        private string _frameName;
        private Actor _actor;
        
        private bool _initialised;
        private bool _isPlaying;
        private bool _isConnected;
        private CollisionData _collisionData;
        
        public bool IsVolatile => EmitterAsset is VolatileEmitterObject;
        
        #endregion

        #region INITIALISATION METHODS
        
        public void Initialise(string frameName, int index, Actor actor)
        {
            _frameName = frameName;
            EntityIndex = index;
            _actor = actor;
        
            if (_initialised)
                return;
            
            if (EmitterAsset == null)
                throw new Exception("EmitterAuth: EmitterAsset is null.");
            
            if (EmitterAsset.AudioAsset == null)
                throw new Exception("EmitterAuth: EmitterAsset.AudioAsset is null.");

            if (PlaybackCondition == PropagateCondition.Volatile && !IsVolatile)
                throw new Exception("EmitterAuth: PlaybackType is Volatile but EmitterAsset is not Volatile.");

            if (PlaybackCondition != PropagateCondition.Volatile && IsVolatile)
                throw new Exception("EmitterAuth: PlaybackType is not Volatile but EmitterAsset is Volatile.");
            
            EmitterAsset.InitialiseParameters(_actor);
            _initialised = true;
        }
        
        public void CreateEntity()
        {
            if (!_initialised)
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
                ReflectPlayhead = ReflectPlayhead,
                AudioClipIndex = EmitterAsset.AudioAsset.ClipEntityIndex,
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
            
            if (!IsVolatile || !_initialised)
                return;

            _isPlaying = true;
        }
        
        public void UpdateEmitterEntity(bool isConnected, int speakerIndex, bool inRange)
        {
            if (!_entityInitialised)
                return;

            _isConnected = isConnected;
            
            if (!_isConnected || !inRange || (IsVolatile && !_isPlaying))
            {
                _isPlaying = false;
                _manager.RemoveComponent<EmitterPlaybackReadyTag>(_emitterEntity);
                return;
            }
            
            if (!IsVolatile)
                _isPlaying = _actor.IsColliding || PlaybackCondition != PropagateCondition.Contact;
            
            if (!_isPlaying)
            {
                _manager.RemoveComponent<EmitterPlaybackReadyTag>(_emitterEntity);
                return;
            }

            var data = _manager.GetComponentData<EmitterComponent>(_emitterEntity);
            UpdateEmitterComponent(ref data);
            _manager.SetComponentData(_emitterEntity, data);
            _manager.AddComponent<EmitterPlaybackReadyTag>(_emitterEntity);
            
            if (IsVolatile)
                _isPlaying = false;
        }
        
        #endregion

        #region UPDATE EMITTER COMPONENT
        
        public void UpdateEmitterComponent(ref EmitterComponent data)
        {
            ModComponent[] modulations = EmitterAsset.BuildModulations(_actor);
            data = new EmitterComponent
            {
                AudioClipIndex = EmitterAsset.AudioAsset.ClipEntityIndex,
                LastSampleIndex = data.LastSampleIndex,
                SamplesUntilFade = _actor.ActorLifeController.SamplesUntilFade(EmitterAsset.AgeFadeOut),
                SamplesUntilDeath = _actor.ActorLifeController.SamplesUntilDeath(),
                LastGrainDuration = data.LastGrainDuration,
                ReflectPlayhead = ReflectPlayhead,
                EmitterVolume = EmitterVolume,
                DynamicAmplitude = Amplitude.CalculateAmplitudeMultiplier(_isConnected, _actor),
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
        public int SamplesUntilFade;
        public int SamplesUntilDeath;
        public int LastGrainDuration;
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