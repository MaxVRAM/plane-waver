using UnityEngine;
using System;

using NaughtyAttributes;

using MaxVRAM;
using MaxVRAM.Extensions;

using PlaneWaver.Modulation;

namespace PlaneWaver
{
    [Serializable]
    public class ModulationInput
    {
        private Actor _localActor;
        
        public void SetLocalActor(Actor localActor)
        {
            _localActor = localActor;
        }

        public void ResetModulation()
        {
            _ValueSource = ModulationSourceGroups.General;
            _SceneProperties = ModulationSourceMisc.Disabled;
            ModulationSourceActor = ModulationSourceActor.Speed;
            _LinkedActors = ModulationSourceRelational.Radius;
            ModulationSourceCollisions = ModulationSourceCollision.CollisionSpeed;
            _InputValue = 0;
            _PreviousSmoothedValue = 0;
            _InputRange = new Vector2(0, 1);
            _AdjustMultiplier = 1;
            _OnNewValue = ModulationAccumulate.Replace;
            _PreSmoothValue = 0;
            _Smoothing = 0.2f;
            _LimiterMode = ModulationLimiter.Clip;
            _ModulationExponent = 1f;
            _ModulationAmount = 0;
            _Result = 0;
            _PreviousVector = Vector3.zero;
        }

        [HorizontalLine(color: EColor.Gray)]
        public ModulationSourceGroups _ValueSource = ModulationSourceGroups.General;

        [ShowIf("ScenePropertySelected")]
        [AllowNesting]
        public ModulationSourceMisc _SceneProperties = ModulationSourceMisc.Disabled;

        [ShowIf("PrimaryActorSelected")]
        [AllowNesting]
        public ModulationSourceActor ModulationSourceActor = ModulationSourceActor.Speed;

        [ShowIf("LinkedActorsSelected")]
        [AllowNesting]
        public ModulationSourceRelational _LinkedActors = ModulationSourceRelational.Radius;

        [ShowIf("CollisionInputSelected")]
        [AllowNesting]
        public ModulationSourceCollision ModulationSourceCollisions = ModulationSourceCollision.CollisionSpeed;

        [SerializeField] private float _InputValue = 0;
        private float _PreviousSmoothedValue = 0;

        [HorizontalLine(color: EColor.Clear)]
        [SerializeField] private Vector2 _InputRange = new (0, 1);
        [SerializeField] private float _AdjustMultiplier = 1;
        [SerializeField] private ModulationAccumulate _OnNewValue = ModulationAccumulate.Replace;
        [SerializeField] private float _PreSmoothValue = 0;

        [HorizontalLine(color: EColor.Clear)]
        [SerializeField][Range(0f, 1f)] private float _Smoothing = 0.2f;
        [SerializeField] private ModulationLimiter _LimiterMode = ModulationLimiter.Clip;
        [ShowIf("ClipLimitingApplied")]
        [AllowNesting]
        [SerializeField][Range(0.5f, 5.0f)] private float _ModulationExponent = 1f;
        [SerializeField][Range(-1f, 1f)] private float _ModulationAmount = 0;

        [HorizontalLine(color: EColor.Gray)]
        [SerializeField][Range(0f, 1f)] private float _Result = 0;

        private Vector3 _PreviousVector = Vector3.zero;

        public float Smoothing => _ValueSource != ModulationSourceGroups.ActorCollisions ? _Smoothing : 0;
        public float Exponent => _ModulationExponent;
        public float Amount => _ModulationAmount;


        public bool ScenePropertySelected() { return _ValueSource == ModulationSourceGroups.General; }
        public bool PrimaryActorSelected() { return _ValueSource == ModulationSourceGroups.PrimaryActor; }
        public bool LinkedActorsSelected() { return _ValueSource == ModulationSourceGroups.LinkedActors; }
        public bool CollisionInputSelected() { return _ValueSource == ModulationSourceGroups.ActorCollisions; }
        public bool ClipLimitingApplied => _LimiterMode == ModulationLimiter.Clip;

        public float GetProcessedValue()
        {
            if (_localActor == null || _ModulationAmount == 0)
                _Result = 0;
            else
            {
                GenerateRawValue();
                _Result = ProcessValue(_InputValue);
            }
            return _Result;
        }

        public float GetProcessedValue(float offset)
        {
            if (_localActor == null || _ModulationAmount == 0)
                _Result = offset;
            else
            {
                GenerateRawValue();
                _Result = ProcessValue(_InputValue, offset);
            }
            return _Result;
        }

        private float ProcessValue(float inputValue)
        {
            float newValue = Mathf.Clamp01(MaxMath.Map(inputValue, _InputRange, 0, 1) * _AdjustMultiplier);
            _PreSmoothValue = _OnNewValue == ModulationAccumulate.Accumulate ? _PreSmoothValue + newValue : newValue;
            newValue = MaxMath.Smooth(_PreviousSmoothedValue, _PreSmoothValue, Smoothing);
            _PreviousSmoothedValue = newValue;

            newValue = _LimiterMode switch
            {
                    ModulationLimiter.Repeat   => newValue.RepeatNorm(),
                    ModulationLimiter.PingPong => newValue.PingPongNorm(),
                    _                     => newValue
            };

            return Mathf.Clamp01(newValue);
        }

        private float ProcessValue(float inputValue, float offset)
        {
            float newValue = MaxMath.Map(inputValue, _InputRange, 0, 1) * _AdjustMultiplier;
            _PreSmoothValue = _OnNewValue == ModulationAccumulate.Accumulate ? _PreSmoothValue + newValue : newValue;
            newValue = MaxMath.Smooth(_PreviousSmoothedValue, _PreSmoothValue, Smoothing);
            _PreviousSmoothedValue = newValue;

            newValue = _LimiterMode switch
            {
                    ModulationLimiter.Clip     => offset + Mathf.Pow(newValue, Exponent) * Amount,
                    ModulationLimiter.Repeat   => newValue.RepeatNorm(Amount, offset),
                    ModulationLimiter.PingPong => newValue.PingPongNorm(Amount, offset),
                    _                     => newValue
            };

            return Mathf.Clamp01(newValue);
        }

        private void GenerateRawValue()
        {
            switch (_ValueSource)
            {
                case ModulationSourceGroups.General:
                    GenerateScenePropertyValue();
                    break;
                case ModulationSourceGroups.PrimaryActor:
                    _localActor.GetActorValue(ref _InputValue, ref _PreviousVector, ModulationSourceActor);
                    break;
                case ModulationSourceGroups.LinkedActors:
                    _localActor.GetActorOtherValue(ref _InputValue, ref _PreviousVector, _LinkedActors);
                    break;
                case ModulationSourceGroups.ActorCollisions:
                    _localActor.GetCollisionValue(ref _InputValue, ModulationSourceCollisions);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void GenerateScenePropertyValue()
        {
            switch (_SceneProperties)
            {
                case ModulationSourceMisc.Disabled:
                    break;
                case ModulationSourceMisc.TimeSinceStart:
                    _InputValue = Time.time;
                    break;
                case ModulationSourceMisc.DeltaTime:
                    _InputValue = Time.deltaTime;
                    break;
                case ModulationSourceMisc.SpawnAge:
                    break;
                case ModulationSourceMisc.SpawnAgeNorm:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
