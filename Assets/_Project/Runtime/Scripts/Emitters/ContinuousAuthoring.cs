using UnityEngine;
using Unity.Entities;
using Serializable = System.SerializableAttribute;
using Random = UnityEngine.Random;

using NaughtyAttributes;

using Ranges = PlaneWaver.EmitterParameterRanges;

namespace PlaneWaver
{
    /// <summary>
    ///  Emitter class for building and spawning a continuous stream of audio grain playback.
    /// </summary>
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

        protected override void SetElementType()
        {
            _EmitterType = EmitterType.Continuous;
            ElementType = SynthElementType.Emitter;
            Archetype = Manager.CreateArchetype(typeof(ContinuousComponent));
            _IsPlaying = _PlaybackCondition != Condition.NotColliding;
            _PlayheadIdle = Random.Range(_PlayheadStartPosition.x, _PlayheadStartPosition.y);
        }

        protected override void InitialiseComponents()
        {
            Manager.AddComponentData(ElementEntity, new ContinuousComponent
            {
                IsPlaying = false,
                EmitterIndex = EntityIndex,
                AudioClipIndex = _AudioAsset.ClipEntityIndex,
                SpeakerIndex = Host.AttachedSpeakerIndex,
                HostIndex = Host.EntityIndex,
                VolumeAdjust = _VolumeAdjust,
                DistanceAmplitude = 1,
                PingPong = _PingPongGrainPlayheads,
                SamplesUntilFade = Host._SpawnLife.GetSamplesUntilFade(_AgeFadeout),
                SamplesUntilDeath = Host._SpawnLife.GetSamplesUntilDeath(),
                LastSampleIndex = -1,

                Volume = new ModulationComponent
                {
                    Min = Ranges._Volume.x,
                    Max = Ranges._Volume.y,
                },
                Playhead = new ModulationComponent
                {
                    Min = Ranges._Playhead.x,
                    Max = Ranges._Playhead.y,
                },
                Duration = new ModulationComponent
                {
                    Min = Ranges._Duration.x,
                    Max = Ranges._Duration.y,
                },
                Density = new ModulationComponent
                {
                    Min = Ranges._Density.x,
                    Max = Ranges._Density.y,
                },
                Transpose = new ModulationComponent
                {
                    Min = Ranges._Transpose.x,
                    Max = Ranges._Transpose.y,
                }
            });

            Manager.AddBuffer<DSPParametersElement>(ElementEntity);
            DynamicBuffer<DSPParametersElement> dspParams = Manager.GetBuffer<DSPParametersElement>(ElementEntity);

            for (int i = 0; i < _DSPChainParams.Length; i++)
                dspParams.Add(_DSPChainParams[i].GetDSPBufferElement());

            Manager.AddComponentData(ElementEntity, new QuadEntityType { _Type = QuadEntityType.QuadEntityTypeEnum.Emitter });
        }

        #endregion

        #region CRAZY CONTINUOUS COMPONENT UPDATE

        protected override void ProcessComponents()
        {
            _IsPlaying = _PlaybackCondition == Condition.Always || _IsPlaying;

            UpdateEntityTags();

            if (IsPlaying)
            {
                ContinuousComponent entity = Manager.GetComponentData<ContinuousComponent>(ElementEntity);
                // Reset grain offset if attached to a new speaker
                if (Host.AttachedSpeakerIndex != entity.SpeakerIndex)
                {
                    _LastSampleIndex = -1;
                    entity.PreviousGrainDuration = -1;
                }

                _LastSampleIndex = entity.LastSampleIndex;

                entity.IsPlaying = IsPlaying;
                entity.AudioClipIndex = _AudioAsset.ClipEntityIndex;
                entity.SpeakerIndex = Host.AttachedSpeakerIndex;
                entity.HostIndex = Host.EntityIndex;
                entity.LastSampleIndex = _LastSampleIndex;
                entity.PingPong = _PingPongGrainPlayheads;
                entity.SamplesUntilFade = Host._SpawnLife.GetSamplesUntilFade(_AgeFadeout);
                entity.SamplesUntilDeath = Host._SpawnLife.GetSamplesUntilDeath();
                entity.VolumeAdjust = _VolumeAdjust;
                entity.DistanceAmplitude = DistanceAmplitude;

                entity.Volume = new ModulationComponent
                {
                    StartValue = VolumeModulated * (_CollisionRigidityScaleVolume ? _ColliderRigidityVolume : 1),
                    Noise = _VolumeNoise.Amount,
                    PerlinNoise = _VolumeNoise._Perlin,
                    PerlinValue = GetPerlinValue(0, _VolumeNoise._Speed),
                    Min = Ranges._Volume.x,
                    Max = Ranges._Volume.y,
                };
                entity.Playhead = new ModulationComponent
                {
                    StartValue = PlayheadModulated,
                    Noise = _PlayheadNoise.Amount,
                    PerlinNoise = _PlayheadNoise._Perlin,
                    PerlinValue = GetPerlinValue(1, _PlayheadNoise._Speed),
                    Min = Ranges._Playhead.x,
                    Max = Ranges._Playhead.y,
                };
                entity.Duration = new ModulationComponent
                {
                    StartValue = DurationModulated,
                    Noise = _DurationNoise.Amount,
                    PerlinNoise = _DurationNoise._Perlin,
                    PerlinValue = GetPerlinValue(2, _DurationNoise._Speed),
                    Min = Ranges._Duration.x,
                    Max = Ranges._Duration.y,
                };
                entity.Density = new ModulationComponent
                {
                    StartValue = DensityModulated,
                    Noise = _DensityNoise.Amount,
                    PerlinNoise = _DensityNoise._Perlin,
                    PerlinValue = GetPerlinValue(3, _DensityNoise._Speed),
                    Min = Ranges._Density.x,
                    Max = Ranges._Density.y,
                };
                entity.Transpose = new ModulationComponent
                {
                    StartValue = TransposeModulated,
                    Noise = _TransposeNoise.Amount,
                    PerlinNoise = _TransposeNoise._Perlin,
                    PerlinValue = GetPerlinValue(4, _TransposeNoise._Speed),
                    Min = Ranges._Transpose.x,
                    Max = Ranges._Transpose.y,
                };
                Manager.SetComponentData(ElementEntity, entity);

                UpdateDSPEffectsBuffer();
            }
        }

        #endregion
    }
}