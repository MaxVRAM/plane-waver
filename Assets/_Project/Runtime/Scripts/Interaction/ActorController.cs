using System;
using UnityEngine;


namespace PlaneWaver.Interaction
{
    public class ActorController : MonoBehaviour
    {
        #region FIELDS & PROPERTIES
        
        private int _sampleRate;
        public bool LiveForever = true;
        public float Lifespan = -1;
        public float Age { get; private set; }
        public float BoundingRadius = 30;
        private ActorBounds _boundingAreaType = ActorBounds.SpawnPosition;
        private Collider _boundingCollider;
        private Transform _boundingTransform;
        private Vector3 _spawnPosition;
        private bool _useCollider;
        private int _outsideFor;
        private const int FramesAllowedOut = 3;
        private Vector3 BoundingPosition =>
            _boundingAreaType == ActorBounds.SpawnPosition ? _spawnPosition : _boundingTransform.position;

        #endregion
        
        #region INITIALISATION
        
        public void InitialiseActorLife(ActorControllerData controllerData)
        {
            Lifespan = controllerData.Lifespan < 0 ? -1 : controllerData.Lifespan;
            LiveForever = Lifespan < 0;
            BoundingRadius = controllerData.BoundingRadius;
            _boundingAreaType = controllerData.BoundingAreaType;
            _boundingCollider = controllerData.BoundingCollider;
            _boundingTransform = controllerData.BoundingTransform;
            Age = 0;
        }

        public void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate;
        }

        public void OnEnable()
        {
            Age = 0;
            _outsideFor = 0;
            _spawnPosition = transform.position;
            InitialiseBounds();
        }
        
        #endregion

        #region UPDATE METHODS

        public void Update()
        {
            Age += Time.deltaTime;

            if ((!LiveForever && Age >= Lifespan) || OutsideBoundsCheck())
                gameObject.SetActive(false);
        }
        
        private bool OutsideBoundsCheck()
        {
            if (_boundingAreaType == ActorBounds.Unrestricted)
                return false;

            if (_useCollider)
                _outsideFor += !_boundingCollider.bounds.Contains(transform.position) ? 1 : -_outsideFor;
            else
                _outsideFor += Mathf.Abs((BoundingPosition - transform.position).magnitude) > BoundingRadius ? 1 : -_outsideFor;
            
            return _outsideFor > FramesAllowedOut;
        }
        
        private void InitialiseBounds()
        {
            _outsideFor = 0;
            _spawnPosition = transform.position;
            
            if (_boundingAreaType is not ActorBounds.ColliderBounds)
                return;

            if (_boundingCollider == null && _boundingTransform.gameObject.TryGetComponent(out _boundingCollider))
            {
                _useCollider = true;
                return;
            }
            
            if (_boundingCollider.GetType() == typeof(SphereCollider))
            {
                BoundingRadius = _boundingCollider.bounds.size.magnitude * 0.5f;
                _boundingTransform = _boundingCollider.transform;
                return;
            }
            
            _useCollider = true;
        }

        #endregion

        #region PROPERTY METHODS
        
        public float NormalisedAge()
        {
            return LiveForever || Lifespan == 0 ? 0 : Age / Lifespan;
        }
        
        public int SamplesUntilFade(float normFadeStart)
        {
            return LiveForever ? int.MaxValue : (int)((Lifespan * normFadeStart - Age) * _sampleRate);
        }

        public int SamplesUntilDeath()
        {
            return LiveForever ? int.MaxValue : (int)(Lifespan - Age) * _sampleRate;
        }

        #endregion
    }
}