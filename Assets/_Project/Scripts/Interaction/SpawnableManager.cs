using UnityEngine;


namespace PlaneWaver
{
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
        [SerializeField] protected Vector3 _SpawnPosition;
        public ObjectSpawner.BoundingArea _BoundingArea;
        private Vector3 _BoundingCentre;
        private Collider _ControllerCollider;
        [SerializeField] private bool _UseColliderBounds = false;
        private int _TicksOutsideBounds = 0;
        readonly int _TicksAllowedOutside = 2;

        public float _BoundingRadius = 10;
        public float CurrentAge => _Age;
        public float CurrentAgeNorm => Mathf.Clamp(_Age / _Lifespan, 0, 1);

        void Start()
        {
            _SampleRate = AudioSettings.outputSampleRate;
            _SpawnTime = Time.time;
            _SpawnPosition = transform.position;
            DefineBoundingRules();
        }

        void Update()
        {
            _Age = Time.time - _SpawnTime;

            if (_BoundingArea == ObjectSpawner.BoundingArea.Controller)
                _BoundingCentre = _ControllerObject.transform.position;

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
            if (_BoundingArea == ObjectSpawner.BoundingArea.Unrestricted)
                return false;

            if (_UseColliderBounds)
                _TicksOutsideBounds = !_ControllerCollider.bounds.Contains(transform.position) ? _TicksOutsideBounds + 1 : 0;
            else
                _TicksOutsideBounds = Mathf.Abs((transform.position - _BoundingCentre).magnitude) > _BoundingRadius ? _TicksOutsideBounds + 1 : 0;

            return _TicksOutsideBounds > _TicksAllowedOutside;
        }

        private void DefineBoundingRules()
        {
            switch (_BoundingArea)
            {
                case ObjectSpawner.BoundingArea.Spawn:
                    _BoundingCentre = _SpawnPosition;
                    break;
                case ObjectSpawner.BoundingArea.ControllerBounds:
                    if (_ControllerObject != null && _ControllerObject.TryGetComponent(out _ControllerCollider))
                    {
                        if (_ControllerCollider.GetType() == typeof(SphereCollider))
                            _BoundingRadius = _ControllerCollider.bounds.size.magnitude / 2;
                        else
                            _UseColliderBounds = true;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
