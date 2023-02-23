using UnityEngine;

namespace PlaneWaver
{
    public enum BoundingArea { Unrestricted, SpawnPosition, ControllerTransform, ColliderBounds }

    /// <summary>
    /// MonoBehaviour component that controls the spawned GameObject's lifespan and interactions with
    /// boundaries.<para />The script is dynamically attached on instantiation by the ObjectSpawner class.
    /// </summary>
    public class SpawnableManager : BehaviourClass
    {
        private int _SampleRate;
        public float _Lifespan = int.MaxValue;
        [SerializeField] protected float _SpawnTime;
        [SerializeField] protected float _Age = -1;
        public BoundingArea _BoundingAreaType;
        public Collider _BoundingCollider;
        public Transform _BoundingTransform;
        private Vector3 _SpawnPosition;
        private bool _UseCollider = false;
        private int _TicksOutsideBounds = 0;
        readonly int _TicksAllowedOutside = 3;

        public float _BoundingRadius = 10;
        public float CurrentAge => _Age;
        public float CurrentAgeNorm => Mathf.Clamp(_Age / _Lifespan, 0, 1);

        void Start()
        {
            _SampleRate = AudioSettings.outputSampleRate;
            _SpawnTime = Time.time;
            _SpawnPosition = transform.position;
            CheckBoundingType();
        }

        void Update()
        {
            _Age = Time.time - _SpawnTime;
            if (_Age >= _Lifespan || OutOfBoundsOnTheFull())
                Destroy(gameObject);
        }

        public int GetSamplesUntilFade(float normFadeStart)
        {
            if (_Lifespan == int.MaxValue)
                return int.MaxValue;
            else
                return (int)((_Lifespan * normFadeStart - _Age) * _SampleRate);
        }

        public int GetSamplesUntilDeath()
        {
            if (_Lifespan == int.MaxValue)
                return int.MaxValue;
            else
                return (int)(_Lifespan - _Age) * _SampleRate;
        }

        private bool OutOfBoundsOnTheFull()
        {
            if (_BoundingAreaType == BoundingArea.Unrestricted)
                return false;

            if (_UseCollider)
                _TicksOutsideBounds = !_BoundingCollider.bounds.Contains(transform.position) ? _TicksOutsideBounds + 1 : 0;
            else
            {
                Vector3 boundingPosition = _BoundingAreaType == BoundingArea.SpawnPosition ? _SpawnPosition : _BoundingTransform.position;
                _TicksOutsideBounds = Mathf.Abs((boundingPosition - transform.position).magnitude) > _BoundingRadius ? _TicksOutsideBounds + 1 : 0;

            }

            return _TicksOutsideBounds > _TicksAllowedOutside;
        }

        private void CheckBoundingType()
        {
            if (_BoundingAreaType == BoundingArea.ControllerTransform)
                _BoundingTransform = _ControllerObject.transform;
            else if (_BoundingAreaType != BoundingArea.ColliderBounds)
                return;

            if (_BoundingCollider != null || _ControllerObject.TryGetComponent(out _BoundingCollider))
            {
                if (_BoundingCollider.GetType() == typeof(SphereCollider))
                {
                    _BoundingRadius = _BoundingCollider.bounds.size.magnitude * 0.5f;
                    _BoundingTransform = _BoundingCollider.transform;
                }
                else
                    _UseCollider = true;
            }
            else
            {
                Debug.LogWarning("SpawnableManager: ColliderBounds selected but no Collider found on Controller.");
            }
        }
    }
}
