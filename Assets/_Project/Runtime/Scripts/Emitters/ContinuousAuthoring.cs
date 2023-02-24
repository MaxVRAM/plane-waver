using UnityEngine;
using Unity.Entities;
using Serializable = System.SerializableAttribute;
using Random = UnityEngine.Random;

using NaughtyAttributes;

using Ranges = PlaneWaver.EmitterParameterRanges;

namespace PlaneWaver
{
    /// <summary>
    //  Emitter class for building and spawning a continuous stream of audio grain playback.
    /// <summary>
    public class ContinuousAuthoring : EmitterAuthoring
    {
        #region MODULATION PARAMETERS
     
        [Serializable]
        public class NoiseModule
        {
            public float Amount => _Influence * _Multiplier;
            [Range(-1f, 1f)] public float _Influence = 0f;
            public float _Multiplier = 0.1f;
            public float _Speed = 1f;
            public bool _Perlin = false;
        }

        // TODO: Need to write a custom drawer to avoid having to duplicate these modules just to display correct ranges in editor.

        [HorizontalLine(color: EColor.Blue)]
        [Range(0f, 2f)] public float _VolumeIdle = 0f;
        public Vector2 _VolumeDefault = new(0, 1);
        public float VolumeIdleNorm => Mathf.InverseLerp(Ranges._Volume.x, Ranges._Volume.y, _VolumeIdle);
        public ModulationInput _VolumeModulation;
        public NoiseModule _VolumeNoise;
        public float VolumeModulated => _VolumeModulation.GetProcessedValue(VolumeIdleNorm);

        [HorizontalLine(color: EColor.Blue)]
        [MinMaxSlider(0f, 1f)] public Vector2 _PlayheadStartPosition = new(0.25f,0.75f);
        [SerializeField] private float _PlayheadIdle = 0.5f;
        public float PlayheadIdleNorm => Mathf.InverseLerp(Ranges._Playhead.x, Ranges._Playhead.y, _PlayheadIdle);
        public ModulationInput _PlayheadModulation;
        public NoiseModule _PlayheadNoise;
        public float PlayheadModulated => _PlayheadModulation.GetProcessedValue(PlayheadIdleNorm);

        [HorizontalLine(color: EColor.Blue)]
        [Range(10f, 500f)] public float _DurationIdle = 80f;
        public float DurationIdleNorm => Mathf.InverseLerp(Ranges._Duration.x, Ranges._Duration.y, _DurationIdle);
        public ModulationInput _DurationModulation;
        public NoiseModule _DurationNoise;
        public float DurationModulated => _DurationModulation.GetProcessedValue(DurationIdleNorm);

        [HorizontalLine(color: EColor.Blue)]
        [Range(1f, 10f)] public float _DensityIdle = 3f;
        public float DensityIdleNorm => Mathf.InverseLerp(Ranges._Density.x, Ranges._Density.y, _DensityIdle);
        public ModulationInput _DensityModulation;
        public NoiseModule _DensityNoise;
        public float DensityModulated => _DensityModulation.GetProcessedValue(DensityIdleNorm);

        [HorizontalLine(color: EColor.Blue)]
        [Range(-3f, 3f)] public float _TransposeIdle = 0f;
        public float TransposeIdleNorm => Mathf.InverseLerp(Ranges._Transpose.x, Ranges._Transpose.y, _TransposeIdle);
        public ModulationInput _TransposeModulation;
        public NoiseModule _TransposeNoise;
        public float TransposeModulated => _TransposeModulation.GetProcessedValue(TransposeIdleNorm);

        public override ModulationInput[] GatherModulationInputs()
        {
            ModulationInput[] modulationInputs = new ModulationInput[5];
            modulationInputs[0] = _VolumeModulation;
            modulationInputs[1] = _PlayheadModulation;
            modulationInputs[2] = _DurationModulation;
            modulationInputs[3] = _DensityModulation;
            modulationInputs[4] = _TransposeModulation;
            return modulationInputs;
        }

        #endregion

        #region CONCISE CONTINUOUS COMPONENT INIT

        public override void SetEntityType()
        {
            _EmitterType = EmitterType.Continuous;
            _EntityType = SynthEntityType.Emitter;
            _Archetype = _EntityManager.CreateArchetype(typeof(ContinuousComponent));
            _IsPlaying = _PlaybackCondition != Condition.NotColliding;
            _PlayheadIdle = Random.Range(_PlayheadStartPosition.x, _PlayheadStartPosition.y);
        }

        public override void InitialiseComponents()
        {
            _EntityManager.AddComponentData(_Entity, new ContinuousComponent
            {
                _IsPlaying = false,
                _EmitterIndex = _EntityIndex,
                _AudioClipIndex = _AudioAsset.ClipEntityIndex,
                _SpeakerIndex = Host.AttachedSpeakerIndex,
                _HostIndex = Host.EntityIndex,
                _VolumeAdjust = _VolumeAdjust,
                _DistanceAmplitude = 1,
                _PingPong = _PingPongGrainPlayheads,
                _SamplesUntilFade = Host._SpawnLife.GetSamplesUntilFade(_AgeFadeout),
                _SamplesUntilDeath = Host._SpawnLife.GetSamplesUntilDeath(),
                _LastSampleIndex = -1,

                _Volume = new ModulationComponent
                {
                    _Min = Ranges._Volume.x,
                    _Max = Ranges._Volume.y,
                },
                _Playhead = new ModulationComponent
                {
                    _Min = Ranges._Playhead.x,
                    _Max = Ranges._Playhead.y,
                },
                _Duration = new ModulationComponent
                {
                    _Min = Ranges._Duration.x,
                    _Max = Ranges._Duration.y,
                },
                _Density = new ModulationComponent
                {
                    _Min = Ranges._Density.x,
                    _Max = Ranges._Density.y,
                },
                _Transpose = new ModulationComponent
                {
                    _Min = Ranges._Transpose.x,
                    _Max = Ranges._Transpose.y,
                }
            });

            _EntityManager.AddBuffer<DSPParametersElement>(_Entity);
            DynamicBuffer<DSPParametersElement> dspParams = _EntityManager.GetBuffer<DSPParametersElement>(_Entity);

            for (int i = 0; i < _DSPChainParams.Length; i++)
                dspParams.Add(_DSPChainParams[i].GetDSPBufferElement());

            _EntityManager.AddComponentData(_Entity, new QuadEntityType { _Type = QuadEntityType.QuadEntityTypeEnum.Emitter });
        }

        #endregion

        #region CRAZY CONTINUOUS COMPONENT UPDATE

        public override void ProcessComponents()
        {
            _IsPlaying = _PlaybackCondition == Condition.Always || _IsPlaying;

            UpdateEntityTags();

            if (IsPlaying)
            {
                ContinuousComponent entity = _EntityManager.GetComponentData<ContinuousComponent>(_Entity);
                // Reset grain offset if attached to a new speaker
                if (Host.AttachedSpeakerIndex != entity._SpeakerIndex)
                {
                    _LastSampleIndex = -1;
                    entity._PreviousGrainDuration = -1;
                }

                _LastSampleIndex = entity._LastSampleIndex;

                entity._IsPlaying = IsPlaying;
                entity._AudioClipIndex = _AudioAsset.ClipEntityIndex;
                entity._SpeakerIndex = Host.AttachedSpeakerIndex;
                entity._HostIndex = Host.EntityIndex;
                entity._LastSampleIndex = _LastSampleIndex;
                entity._PingPong = _PingPongGrainPlayheads;
                entity._SamplesUntilFade = Host._SpawnLife.GetSamplesUntilFade(_AgeFadeout);
                entity._SamplesUntilDeath = Host._SpawnLife.GetSamplesUntilDeath();
                entity._VolumeAdjust = _VolumeAdjust;
                entity._DistanceAmplitude = DistanceAmplitude;

                entity._Volume = new ModulationComponent
                {
                    _StartValue = VolumeModulated * (_CollisionRigidityScaleVolume ? _ColliderRigidityVolume : 1),
                    _Noise = _VolumeNoise.Amount,
                    _PerlinNoise = _VolumeNoise._Perlin,
                    _PerlinValue = GetPerlinValue(0, _VolumeNoise._Speed),
                    _Min = Ranges._Volume.x,
                    _Max = Ranges._Volume.y,
                };
                entity._Playhead = new ModulationComponent
                {
                    _StartValue = PlayheadModulated,
                    _Noise = _PlayheadNoise.Amount,
                    _PerlinNoise = _PlayheadNoise._Perlin,
                    _PerlinValue = GetPerlinValue(1, _PlayheadNoise._Speed),
                    _Min = Ranges._Playhead.x,
                    _Max = Ranges._Playhead.y,
                };
                entity._Duration = new ModulationComponent
                {
                    _StartValue = DurationModulated,
                    _Noise = _DurationNoise.Amount,
                    _PerlinNoise = _DurationNoise._Perlin,
                    _PerlinValue = GetPerlinValue(2, _DurationNoise._Speed),
                    _Min = Ranges._Duration.x,
                    _Max = Ranges._Duration.y,
                };
                entity._Density = new ModulationComponent
                {
                    _StartValue = DensityModulated,
                    _Noise = _DensityNoise.Amount,
                    _PerlinNoise = _DensityNoise._Perlin,
                    _PerlinValue = GetPerlinValue(3, _DensityNoise._Speed),
                    _Min = Ranges._Density.x,
                    _Max = Ranges._Density.y,
                };
                entity._Transpose = new ModulationComponent
                {
                    _StartValue = TransposeModulated,
                    _Noise = _TransposeNoise.Amount,
                    _PerlinNoise = _TransposeNoise._Perlin,
                    _PerlinValue = GetPerlinValue(4, _TransposeNoise._Speed),
                    _Min = Ranges._Transpose.x,
                    _Max = Ranges._Transpose.y,
                };
                _EntityManager.SetComponentData(_Entity, entity);

                UpdateDSPEffectsBuffer();
            }
        }

        #endregion
    }
}