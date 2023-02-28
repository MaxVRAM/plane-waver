using System;
using System.Collections.Generic;

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
        
        public PropagateCondition Propagation;
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

            if (Propagation == PropagateCondition.Volatile && !IsVolatile)
                throw new Exception("EmitterAuth: PlaybackType is Volatile but EmitterAsset is not Volatile.");

            if (Propagation != PropagateCondition.Volatile && IsVolatile)
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
                _frameName + "." + EntityIndex + "." + Enum.GetName(typeof(PropagateCondition), Propagation)
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
        
        public void UpdateEmitterEntity(bool isConnected, int speakerIndex)
        {
            _isConnected = isConnected;
            
            if (!_entityInitialised)
                return;
            
            
            if (IsVolatile)
            {
                if (_isPlaying)
                {
                    if (!_isConnected)
                    {
                        _manager.RemoveComponent<EmitterPlaybackReadyTag>(_emitterEntity);
                        _isConnected = false;
                    }
                }
                else
                {
                    if (_isConnected)
                    {
                        _manager.AddComponentData(_emitterEntity, new EmitterPlaybackReadyTag());
                        _isConnected = true;
                    }
                }
            }
            
            _manager.SetComponentData(_emitterEntity, UpdateEmitterComponent());
            
            if (IsVolatile)
                _isPlaying = false;
        }
        
        #endregion

        #region UPDATE EMITTER COMPONENT
        
        public EmitterComponent UpdateEmitterComponent()
        {
            IEnumerable<ModComponent> modulations = EmitterAsset.BuildModulations(_actor);
            
            return new EmitterComponent
            {
                AudioClipIndex = EmitterAsset.AudioAsset.ClipEntityIndex,
                LastSampleIndex = -1,
                SamplesUntilFade = -1,
                SamplesUntilDeath = -1,
                LastGrainDuration = -1,
                ReflectPlayhead = ReflectPlayhead,
                EmitterVolume = EmitterVolume,
                DynamicAmplitude = Amplitude.CalculateAmplitudeMultiplier(_isConnected, _actor),
                ParamVolume = EmitterAsset.ParamVolume.GetModComponent(),
                ParamPlayhead = EmitterAsset.ParamPlayhead.GetModComponent(),
                ParamDuration = EmitterAsset.ParamDuration.GetModComponent(),
                ParamDensity = EmitterAsset.ParamDensity.GetModComponent(),
                ParamTranspose = EmitterAsset.ParamTranspose.GetModComponent(),
                ParamLength = IsVolatile ? EmitterAsset.ParamLength.GetModComponent() : new ModComponent()
            };
        }
        
        #endregion
    }

    public struct EmitterComponent : IComponentData
    {
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
    public struct AloneOnSpeakerTag : IComponentData { }
    public struct EmitterVolatileTag : IComponentData { }
}