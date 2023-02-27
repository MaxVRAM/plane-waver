using System;
using Unity.Entities;

using NaughtyAttributes;
using UnityEngine;

using Ranges = PlaneWaver.EmitterParameterRanges;

namespace PlaneWaver
{
    /// <summary>
    ///  Emitter class for building and spawning bursts of audio grains.
    /// </summary>
    public class BurstAuthoring : EmitterAuthoring
    {
        #region MODULATION PARAMETERS

        [Serializable]
        public class NoiseModule
        {
            public float Amount => _Influence * _Multiplier;
            [Range(-1f, 1f)] public float _Influence = 0f;
            public float _Multiplier = 0.1f;
            public bool _HoldForBurstDuration = false;
        }

        [HorizontalLine(color: EColor.Gray)]
        private readonly Vector2 _VolumePath = new(0f, 0f);
        private readonly bool _VolumeFixedStart = false;
        private readonly bool _VolumeFixedEnd = true;
        public ModulationInput _VolumeModulation;
        public NoiseModule _VolumeNoise;

        [HorizontalLine(color: EColor.Gray)]
        [Range(10f, 1000f)] public float _LengthDefault = 200f;
        private readonly bool _LengthFixedStart = false;
        private readonly bool _LengthFixedEnd = false;
        public ModulationInput _LengthModulation;
        public NoiseModule _LengthNoise;

        [HorizontalLine(color: EColor.Gray)]
        [MinMaxSlider(0f, 1f)] public Vector2 _PlayheadPath = new (0f, 0.5f);
        public bool _PlayheadFixedStart = true;
        public bool _PlayheadFixedEnd = false;
        public ModulationInput _PlayheadModulation;
        public NoiseModule _PlayheadNoise;

        [HorizontalLine(color: EColor.Gray)]
        [MinMaxSlider(10f, 500f)] public Vector2 _DurationPath = new (80f, 120f);
        public bool _DurationFixedStart = false;
        public bool _DurationFixedEnd = true;
        public ModulationInput _DurationModulation;
        public NoiseModule _DurationNoise;

        [HorizontalLine(color: EColor.Gray)]
        [MinMaxSlider(1f, 10f)] public Vector2 _DensityPath = new (2f, 3f);
        public bool _DensityFixedStart = false;
        public bool _DensityFixedEnd = false;
        public ModulationInput _DensityModulation;
        public NoiseModule _DensityNoise;

        [HorizontalLine(color: EColor.Gray)]
        [MinMaxSlider(-3f, 3f)] public Vector2 _TransposePath = new(0f, 0f);
        public bool _TransposeFixedStart = false;
        public bool _TransposeFixedEnd = false;
        public ModulationInput _TransposeModulation;
        public NoiseModule _TransposeNoise;

        public override ModulationInput[] GatherModulationInputs()
        {
            ModulationInput[] modulationInputs = new ModulationInput[6];
            modulationInputs[0] = _LengthModulation;
            modulationInputs[1] = _VolumeModulation;
            modulationInputs[2] = _PlayheadModulation;
            modulationInputs[3] = _DurationModulation;
            modulationInputs[4] = _DensityModulation;
            modulationInputs[5] = _TransposeModulation;
            return modulationInputs;
        }

        #endregion

        #region BANGIN BURST COMPONENT INIT

        protected override void SetElementType()
        {
            _EmitterType = EmitterType.Burst;
            ElementType = SynthElementType.Emitter;
            Archetype = Manager.CreateArchetype(typeof(BurstComponent));
            _IsPlaying = false;
        }

        protected override void InitialiseComponents()
        {
            Manager.AddComponentData(ElementEntity, new BurstComponent
            {
                IsPlaying = false,
                EmitterIndex = EntityIndex,
                AudioClipIndex = _AudioAsset.ClipEntityIndex,
                SpeakerIndex = Host.AttachedSpeakerIndex,
                HostIndex = Host.EntityIndex,
                VolumeAdjust = _VolumeAdjust,
                DistanceAmplitude = 1,
                PingPong = _PingPongGrainPlayheads,

                Volume = new ModulationComponent
                {
                    StartValue = _VolumePath.x,
                    EndValue = _VolumePath.y,
                    Modulation = _VolumeModulation.Amount,
                    Exponent = _VolumeModulation.Exponent,
                    Noise = _VolumeNoise.Amount,
                    LockNoise = _VolumeNoise._HoldForBurstDuration,
                    Min = Ranges._Volume.x,
                    Max = Ranges._Volume.y,
                    FixedStart = _VolumeFixedStart,
                    FixedEnd = _VolumeFixedEnd
                },
                Length = new ModulationComponent
                {
                    StartValue = _LengthDefault,
                    Modulation = _LengthModulation.Amount,
                    Exponent = _LengthModulation.Exponent,
                    Noise = _LengthNoise.Amount,
                    LockNoise = true,
                    Min = Ranges._Length.x,
                    Max = Ranges._Length.y,
                    FixedStart = _LengthFixedStart,
                    FixedEnd = _LengthFixedStart
                },
                Playhead = new ModulationComponent
                {
                    StartValue = _PlayheadPath.x,
                    EndValue = _PlayheadPath.y,
                    Modulation = _PlayheadModulation.Amount,
                    Exponent = _PlayheadModulation.Exponent,
                    Noise = _PlayheadNoise.Amount,
                    LockNoise = _PlayheadNoise._HoldForBurstDuration,
                    Min = Ranges._Playhead.x,
                    Max = Ranges._Playhead.y,
                    FixedStart = _PlayheadFixedStart,
                    FixedEnd = _PlayheadFixedEnd
                },
                Duration = new ModulationComponent
                {
                    StartValue = _DurationPath.x,
                    EndValue = _DurationPath.y,
                    Modulation = _DurationModulation.Amount,
                    Exponent = _DurationModulation.Exponent,
                    Noise = _DurationNoise.Amount,
                    LockNoise = _DurationNoise._HoldForBurstDuration,
                    Min = Ranges._Duration.x,
                    Max = Ranges._Duration.y,
                    FixedStart = _DurationFixedStart,
                    FixedEnd = _DurationFixedEnd
                },
                Density = new ModulationComponent
                {
                    StartValue = _DensityPath.x,
                    EndValue = _DensityPath.y,
                    Modulation = _DensityModulation.Amount,
                    Exponent = _DensityModulation.Exponent,
                    Noise = _DensityNoise.Amount,
                    Min = Ranges._Density.x,
                    Max = Ranges._Density.y,
                    FixedStart = _DensityFixedStart,
                    FixedEnd = _DensityFixedEnd
                },
                Transpose = new ModulationComponent
                {
                    StartValue = _TransposePath.x,
                    EndValue = _TransposePath.y,
                    Modulation = _TransposeModulation.Amount,
                    Exponent = _TransposeModulation.Exponent,
                    Noise = _TransposeNoise.Amount,
                    Min = Ranges._Transpose.x,
                    Max = Ranges._Transpose.y,
                    FixedStart = _TransposeFixedStart,
                    FixedEnd = _TransposeFixedEnd
                }
            });

            Manager.AddBuffer<DSPParametersElement>(ElementEntity);
            DynamicBuffer<DSPParametersElement> dspParams = Manager.GetBuffer<DSPParametersElement>(ElementEntity);

            for (int i = 0; i < _DSPChainParams.Length; i++)
                dspParams.Add(_DSPChainParams[i].GetDSPBufferElement());

            Manager.AddComponentData(ElementEntity, new QuadEntityType { _Type = QuadEntityType.QuadEntityTypeEnum.Emitter });
        }

        #endregion

        #region BUOYANT BURST COMPONENT UPDATE

        protected override void ProcessComponents()
        {
            UpdateEntityTags();

            if (_IsPlaying)
            {
                BurstComponent entity = Manager.GetComponentData<BurstComponent>(ElementEntity);

                entity.IsPlaying = true;
                entity.AudioClipIndex = _AudioAsset.ClipEntityIndex;
                entity.SpeakerIndex = Host.AttachedSpeakerIndex;
                entity.HostIndex = Host.EntityIndex;
                entity.PingPong = _PingPongGrainPlayheads;
                entity.VolumeAdjust = _VolumeAdjust;
                entity.DistanceAmplitude = DistanceAmplitude;

                entity.Volume = new ModulationComponent
                {
                    StartValue = _VolumePath.x * (_CollisionRigidityScaleVolume ? _ColliderRigidityVolume : 1),
                    EndValue = _VolumePath.y * (_CollisionRigidityScaleVolume ? _ColliderRigidityVolume : 1),
                    Modulation = _VolumeModulation.Amount,
                    Exponent = _VolumeModulation.Exponent,
                    Noise = _VolumeNoise.Amount,
                    LockNoise = _VolumeNoise._HoldForBurstDuration,
                    Min = Ranges._Volume.x,
                    Max = Ranges._Volume.y,
                    FixedStart = _VolumeFixedStart,
                    FixedEnd = _VolumeFixedEnd,
                    Input = _VolumeModulation.GetProcessedValue()
                };
                entity.Length = new ModulationComponent
                {
                    StartValue = _LengthDefault,
                    Modulation = _LengthModulation.Amount,
                    Exponent = _LengthModulation.Exponent,
                    Noise = _LengthNoise.Amount,
                    LockNoise = true,
                    Min = Ranges._Length.x,
                    Max = Ranges._Length.y,
                    FixedStart = _LengthFixedStart,
                    FixedEnd = _LengthFixedEnd,
                    Input = _LengthModulation.GetProcessedValue()
                };
                entity.Playhead = new ModulationComponent
                {
                    StartValue = _PlayheadPath.x,
                    EndValue = _PlayheadPath.y,
                    Modulation = _PlayheadModulation.Amount,
                    Exponent = _PlayheadModulation.Exponent,
                    Noise = _PlayheadNoise.Amount,
                    LockNoise = _PlayheadNoise._HoldForBurstDuration,
                    Min = Ranges._Playhead.x,
                    Max = Ranges._Playhead.y,
                    FixedStart = _PlayheadFixedStart,
                    FixedEnd = _PlayheadFixedEnd,
                    Input = _PlayheadModulation.GetProcessedValue()
                };
                entity.Duration = new ModulationComponent
                {
                    StartValue = _DurationPath.x,
                    EndValue = _DurationPath.y,
                    Modulation = _DurationModulation.Amount,
                    Exponent = _DurationModulation.Exponent,
                    Noise = _DurationNoise.Amount,
                    LockNoise = _DurationNoise._HoldForBurstDuration,
                    Min = Ranges._Duration.x,
                    Max = Ranges._Duration.y,
                    FixedStart = _DurationFixedStart,
                    FixedEnd = _DurationFixedEnd,
                    Input = _DurationModulation.GetProcessedValue()
                };
                entity.Density = new ModulationComponent
                {
                    StartValue = _DensityPath.x,
                    EndValue = _DensityPath.y,
                    Modulation = _DensityModulation.Amount,
                    Exponent = _DensityModulation.Exponent,
                    Noise = _DensityNoise.Amount,
                    LockNoise = _DensityNoise._HoldForBurstDuration,
                    Min = Ranges._Density.x,
                    Max = Ranges._Density.y,
                    FixedStart = _DensityFixedStart,
                    FixedEnd = _DensityFixedEnd,
                    Input = _DensityModulation.GetProcessedValue()
                };
                entity.Transpose = new ModulationComponent
                {
                    StartValue = _TransposePath.x,
                    EndValue = _TransposePath.y,
                    Modulation = _TransposeModulation.Amount,
                    Exponent = _TransposeModulation.Exponent,
                    Noise = _TransposeNoise.Amount,
                    LockNoise = _TransposeNoise._HoldForBurstDuration,
                    Min = Ranges._Transpose.x,
                    Max = Ranges._Transpose.y,
                    FixedStart = _TransposeFixedStart,
                    FixedEnd = _TransposeFixedEnd,
                    Input = _TransposeModulation.GetProcessedValue()
                };
                Manager.SetComponentData(ElementEntity, entity);

                UpdateDSPEffectsBuffer();
                // Burst emitters generate their entire output in one pass, so switching off
                _IsPlaying = false;
            }
        }

        #endregion
    }
}
