
using UnityEngine;

namespace PlaneWaver
{
    public class ActorLife : MonoBehaviour
    {
        #region CLASS DEFINITIONS
        
        private int _sampleRate;
        public bool LiveForever = true;
        public float Lifespan = -1;
        private float _age;
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
        
        #region INITIALISATION METHODS
        
        public void InitialiseActorLife(ActorLifeData actorLifeData)
        {
            if (actorLifeData.Lifespan < 0)
                LiveForever = true;
            Lifespan = actorLifeData.Lifespan;
            BoundingRadius = actorLifeData.BoundingRadius;
            _boundingAreaType = actorLifeData.BoundingAreaType;
            _boundingCollider = actorLifeData.BoundingCollider;
            _boundingTransform = actorLifeData.BoundingTransform;
            _age = 0;
        }
        
        #endregion

        #region RUNTIME UPDATES
        
        public void Start()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            InitialiseBounds();
        }
        
        public void Update()
        {
            _age += Time.deltaTime;

            if ((!LiveForever && _age >= Lifespan) || OutsideBoundsCheck())
                Destroy(gameObject);
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

        #region PROPERTY RETURN METHODS
        
        public int GetSamplesUntilFade(float normFadeStart)
        {
            return LiveForever ? int.MaxValue : (int)((Lifespan * normFadeStart - _age) * _sampleRate);
        }

        public int GetSamplesUntilDeath()
        {
            return LiveForever ? int.MaxValue : (int)(Lifespan - _age) * _sampleRate;
        }

        #endregion
    }

    #region PUBLIC ACTOR TYPE DEFINITIONS

    public enum ActorBounds
    {
        Unrestricted, SpawnPosition, ControllerTransform, ColliderBounds
    }
    
    public struct ActorLifeData
    {
        public readonly float Lifespan;
        public readonly float BoundingRadius;
        public readonly ActorBounds BoundingAreaType;
        public readonly Collider BoundingCollider;
        public readonly Transform BoundingTransform;
        
        public ActorLifeData(
                float lifespan, 
                float boundingRadius,
                ActorBounds boundingAreaType, 
                Collider boundingCollider, 
                Transform boundingTransform)
        {
            Lifespan = lifespan;
            BoundingRadius = boundingRadius;
            BoundingAreaType = boundingAreaType;
            BoundingCollider = boundingCollider;
            BoundingTransform = boundingTransform;
        }
    }
    
    #endregion
}