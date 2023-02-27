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
            _ValueSource = InputSourceGroups.General;
            _SceneProperties = GeneralSources.StaticValue;
            _PrimaryActor = PrimaryActorSources.Speed;
            _LinkedActors = LinkedActorSources.Radius;
            _ActorCollisions = ActorCollisionSources.CollisionSpeed;
            _InputValue = 0;
            _PreviousSmoothedValue = 0;
            _InputRange = new Vector2(0, 1);
            _AdjustMultiplier = 1;
            _OnNewValue = InputOnNewValue.Replace;
            _PreSmoothValue = 0;
            _Smoothing = 0.2f;
            _LimiterMode = ValueLimiter.Clip;
            _ModulationExponent = 1f;
            _ModulationAmount = 0;
            _Result = 0;
            _PreviousVector = Vector3.zero;
        }

        [HorizontalLine(color: EColor.Gray)]
        public InputSourceGroups _ValueSource = InputSourceGroups.General;

        [ShowIf("ScenePropertySelected")]
        [AllowNesting]
        public GeneralSources _SceneProperties = GeneralSources.StaticValue;

        [ShowIf("PrimaryActorSelected")]
        [AllowNesting]
        public PrimaryActorSources _PrimaryActor = PrimaryActorSources.Speed;

        [ShowIf("LinkedActorsSelected")]
        [AllowNesting]
        public LinkedActorSources _LinkedActors = LinkedActorSources.Radius;

        [ShowIf("CollisionInputSelected")]
        [AllowNesting]
        public ActorCollisionSources _ActorCollisions = ActorCollisionSources.CollisionSpeed;

        [SerializeField] private float _InputValue = 0;
        private float _PreviousSmoothedValue = 0;

        [HorizontalLine(color: EColor.Clear)]
        [SerializeField] private Vector2 _InputRange = new (0, 1);
        [SerializeField] private float _AdjustMultiplier = 1;
        [SerializeField] private InputOnNewValue _OnNewValue = InputOnNewValue.Replace;
        [SerializeField] private float _PreSmoothValue = 0;

        [HorizontalLine(color: EColor.Clear)]
        [SerializeField][Range(0f, 1f)] private float _Smoothing = 0.2f;
        [SerializeField] private ValueLimiter _LimiterMode = ValueLimiter.Clip;
        [ShowIf("ClipLimitingApplied")]
        [AllowNesting]
        [SerializeField][Range(0.5f, 5.0f)] private float _ModulationExponent = 1f;
        [SerializeField][Range(-1f, 1f)] private float _ModulationAmount = 0;

        [HorizontalLine(color: EColor.Gray)]
        [SerializeField][Range(0f, 1f)] private float _Result = 0;

        private Vector3 _PreviousVector = Vector3.zero;

        public float Smoothing => _ValueSource != InputSourceGroups.ActorCollisions ? _Smoothing : 0;
        public float Exponent => _ModulationExponent;
        public float Amount => _ModulationAmount;


        public bool ScenePropertySelected() { return _ValueSource == InputSourceGroups.General; }
        public bool PrimaryActorSelected() { return _ValueSource == InputSourceGroups.PrimaryActor; }
        public bool LinkedActorsSelected() { return _ValueSource == InputSourceGroups.LinkedActors; }
        public bool CollisionInputSelected() { return _ValueSource == InputSourceGroups.ActorCollisions; }
        public bool ClipLimitingApplied => _LimiterMode == ValueLimiter.Clip;

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
            _PreSmoothValue = _OnNewValue == InputOnNewValue.Accumulate ? _PreSmoothValue + newValue : newValue;
            newValue = MaxMath.Smooth(_PreviousSmoothedValue, _PreSmoothValue, Smoothing);
            _PreviousSmoothedValue = newValue;

            newValue = _LimiterMode switch
            {
                    ValueLimiter.Repeat   => newValue.RepeatNorm(),
                    ValueLimiter.PingPong => newValue.PingPongNorm(),
                    _                     => newValue
            };

            return Mathf.Clamp01(newValue);
        }

        private float ProcessValue(float inputValue, float offset)
        {
            float newValue = MaxMath.Map(inputValue, _InputRange, 0, 1) * _AdjustMultiplier;
            _PreSmoothValue = _OnNewValue == InputOnNewValue.Accumulate ? _PreSmoothValue + newValue : newValue;
            newValue = MaxMath.Smooth(_PreviousSmoothedValue, _PreSmoothValue, Smoothing);
            _PreviousSmoothedValue = newValue;

            newValue = _LimiterMode switch
            {
                    ValueLimiter.Clip     => offset + Mathf.Pow(newValue, Exponent) * Amount,
                    ValueLimiter.Repeat   => newValue.RepeatNorm(Amount, offset),
                    ValueLimiter.PingPong => newValue.PingPongNorm(Amount, offset),
                    _                     => newValue
            };

            return Mathf.Clamp01(newValue);
        }

        private void GenerateRawValue()
        {
            switch (_ValueSource)
            {
                case InputSourceGroups.General:
                    GenerateScenePropertyValue();
                    break;
                case InputSourceGroups.PrimaryActor:
                    _localActor.GetActorValue(ref _InputValue, ref _PreviousVector, _PrimaryActor);
                    break;
                case InputSourceGroups.LinkedActors:
                    _localActor.GetActorOtherValue(ref _InputValue, ref _PreviousVector, _LinkedActors);
                    break;
                case InputSourceGroups.ActorCollisions:
                    _localActor.GetCollisionValue(ref _InputValue, _ActorCollisions);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void GenerateScenePropertyValue()
        {
            switch (_SceneProperties)
            {
                case GeneralSources.StaticValue:
                    break;
                case GeneralSources.TimeSinceStart:
                    _InputValue = Time.time;
                    break;
                case GeneralSources.DeltaTime:
                    _InputValue = Time.deltaTime;
                    break;
                case GeneralSources.SpawnAge:
                    break;
                case GeneralSources.SpawnAgeNorm:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
