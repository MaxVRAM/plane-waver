using UnityEngine;
using System;

using MaxVRAM;
using MaxVRAM.Extensions;
using NaughtyAttributes;

namespace PlaneWaver
{
    [Serializable]
    public class ModulationInput
    {
        private Actor _LocalActor;
        private Actor _RemoteActor;

        public void SetLocalActor(Actor localActor)
        {
            _LocalActor = localActor;
        }

        public void SetRemoteActor(Actor remoteActor)
        {
            _RemoteActor = remoteActor;
        }

        public void SetActors(Actor localActor, Actor remoteActor)
        {
            _LocalActor = localActor;
            _RemoteActor = remoteActor;
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
            _InputRange = new(0, 1);
            _AdjustMultiplier = 1;
            _OnNewValue = InputOnNewValue.Replace;
            _PreSmoothValue = 0;
            _Smoothing = 0.2f;
            _LimiterMode = InputLimitMode.Clip;
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
        [SerializeField] private InputLimitMode _LimiterMode = InputLimitMode.Clip;
        [ShowIf("ClipLimitingApplied")]
        [AllowNesting]
        [SerializeField][Range(0.5f, 5.0f)] private float _ModulationExponent = 1f;

        [HorizontalLine(color: EColor.Gray)]
        [SerializeField][Range(-1f, 1f)] private float _ModulationAmount = 0;
        [SerializeField][Range(0f, 1f)] private float _Result = 0;

        private Vector3 _PreviousVector = Vector3.zero;

        public float Smoothing => _ValueSource != InputSourceGroups.ActorCollisions ? _Smoothing : 0;
        public float Exponent => _ModulationExponent;
        public float Amount => _ModulationAmount;


        public bool ScenePropertySelected() { return _ValueSource == InputSourceGroups.General; }
        public bool PrimaryActorSelected() { return _ValueSource == InputSourceGroups.PrimaryActor; }
        public bool LinkedActorsSelected() { return _ValueSource == InputSourceGroups.LinkedActors; }
        public bool CollisionInputSelected() { return _ValueSource == InputSourceGroups.ActorCollisions; }
        public bool ClipLimitingApplied => _LimiterMode == InputLimitMode.Clip;

        public float GetProcessedValue()
        {
            if (_LocalActor == null || _ModulationAmount == 0)
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
            if (_LocalActor == null || _ModulationAmount == 0)
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
            float newValue = MaxMath.Map(inputValue, _InputRange, 0, 1) * _AdjustMultiplier;
            _PreSmoothValue = _OnNewValue == InputOnNewValue.Accumulate ? _PreSmoothValue + newValue : newValue;
            newValue = MaxMath.Smooth(_PreviousSmoothedValue, _PreSmoothValue, Smoothing, Time.deltaTime);
            _PreviousSmoothedValue = newValue;

            if (_LimiterMode == InputLimitMode.Repeat)
                newValue = newValue.RepeatNorm();
            else if (_LimiterMode == InputLimitMode.PingPong)
                newValue = newValue.PingPongNorm();

            return Mathf.Clamp01(newValue);
        }

        private float ProcessValue(float inputValue, float offset)
        {
            float newValue = MaxMath.Map(inputValue, _InputRange, 0, 1) * _AdjustMultiplier;
            _PreSmoothValue = _OnNewValue == InputOnNewValue.Accumulate ? _PreSmoothValue + newValue : newValue;
            newValue = MaxMath.Smooth(_PreviousSmoothedValue, _PreSmoothValue, Smoothing, Time.deltaTime);
            _PreviousSmoothedValue = newValue;

            if (_LimiterMode == InputLimitMode.Clip)
                newValue = offset + Mathf.Pow(newValue, Exponent) * Amount;
            else if (_LimiterMode == InputLimitMode.Repeat)
                newValue = newValue.RepeatNorm(Amount, offset);
            else if (_LimiterMode == InputLimitMode.PingPong)
                newValue = newValue.PingPongNorm(Amount, offset);

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
                    _LocalActor.GetActorValue(ref _InputValue, ref _PreviousVector, _PrimaryActor);
                    break;
                case InputSourceGroups.LinkedActors:
                    _LocalActor.GetActorPairValue(ref _InputValue, ref _PreviousVector, _RemoteActor, _LinkedActors);
                    break;
                case InputSourceGroups.ActorCollisions:
                    _LocalActor.GetCollisionValue(ref _InputValue, _ActorCollisions);
                    break;
                default:
                    break;
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
                    break;
            }
        }
    }
}
