using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

using MaxVRAM.Audio;
using MaxVRAM.Extensions;
using NaughtyAttributes;

namespace PlaneWaver
{
    /// <summary>
    ///      Multiple emitters are often spawned and attached to the same object and modulated by
    ///      the same game-world interactions. Emitter Hosts manage emitters designed to create
    ///      a single "profile" for an interactive sound object.
    /// </summary>
    public class HostAuthoring : SynthElement
    {
        #region FIELDS & PROPERTIES

        private Transform _headTransform;
        [AllowNesting][BoxGroup("Speaker Attachment")]
        [Tooltip("Parent transform position for speakers to target for this host. Creates a new child transform if not provided.")]
        public Transform SpeakerTarget;
        [AllowNesting][BoxGroup("Speaker Attachment")]
        [Tooltip("Sets to the connected speaker's transform. Sets to this host's Speaker Target if no speaker is connected.")]
        public Transform SpeakerTransform;
        [AllowNesting][BoxGroup("Speaker Attachment")]
        public MaterialColourModulator MaterialModulator = new();

        [AllowNesting]
        [BoxGroup("Interaction")]
        public ObjectSpawner _Spawner;
        [AllowNesting]
        [BoxGroup("Interaction")]
        public SpawnableManager _SpawnLife;
        [AllowNesting]
        [BoxGroup("Interaction")]
        public Transform _LocalTransform;
        public Actor _LocalActor;
        [AllowNesting]
        [BoxGroup("Interaction")]
        public Transform _RemoteTransform;
        public Actor _RemoteActor;
        [AllowNesting]
        [BoxGroup("Interaction")]
        [SerializeField] private float _SelfRigidity = 0.5f;
        [BoxGroup("Interaction")]
        [AllowNesting]
        [SerializeField] private float _EaseCollidingRigidity = 0.5f;
        [AllowNesting]
        [BoxGroup("Interaction")]
        [SerializeField] private float _CollidingRigidity = 0;
        private float _TargetCollidingRigidity = 0;

        public float SurfaceRigidity => _SelfRigidity;
        public float RigiditySmoothUp => 1 / _EaseCollidingRigidity;
        public float CollidingRigidity => _CollidingRigidity;

        private SurfaceProperties _SurfaceProperties;

        [HorizontalLine(color: EColor.Gray)]
        public List<EmitterAuthoring> _HostedEmitters;
        public List<GameObject> _CollidingObjects;

        [AllowNesting]
        [Foldout("Runtime Dynamics")]
        [SerializeField] private bool _Connected = false;
        [AllowNesting]
        [Foldout("Runtime Dynamics")]
        [SerializeField] private int _AttachedSpeakerIndex = int.MaxValue;
        [AllowNesting]
        [Foldout("Runtime Dynamics")]
        [SerializeField] private bool _InListenerRadius = false;
        [AllowNesting]
        [Foldout("Runtime Dynamics")]
        [SerializeField] private float _ListenerDistance = 0;
        [AllowNesting]
        [Foldout("Runtime Dynamics")]
        public bool _IsColliding = false;

        private bool _RemotelyAssigned = false;

        public bool IsConnected => _Connected;
        public int AttachedSpeakerIndex => _AttachedSpeakerIndex;
        public bool InListenerRadius => _InListenerRadius;

        #endregion

        #region ENTITY-SPECIFIC START CALL

        void Start()
        {
            InitialiseModules();

            Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Archetype = Manager.CreateArchetype(typeof(HostComponent));
            SetIndex(GrainBrain.Instance.RegisterHost(this));
        }

        public void InitialiseModules()
        {
            if (!_RemotelyAssigned)
            {
                _LocalTransform = transform;
                if (!_LocalTransform.TryGetComponent(out _SpawnLife))
                    _SpawnLife = _LocalTransform.gameObject.AddComponent<SpawnableManager>();
            }

            // _LocalActor = new(_LocalTransform);
            // _RemoteActor = _RemoteTransform != null ? new(_RemoteTransform) : null;

            InitialiseSpeakerAttachment();
            InitialiseSurfaceProperties();

            if (MaterialModulator._Renderer == null)
                MaterialModulator._Renderer = _LocalTransform.GetComponentInChildren<Renderer>();

            _headTransform = FindObjectOfType<Camera>().transform;

            _HostedEmitters.AddRange(_LocalTransform.GetComponentsInChildren<EmitterAuthoring>());

            foreach (EmitterAuthoring emitter in _HostedEmitters)
                emitter.InitialiseByHost(this);
        }

        private void InitialiseSpeakerAttachment()
        {
            if (SpeakerTarget == null || SpeakerTarget == _LocalTransform || SpeakerTarget == transform)
            {
                SpeakerTarget = new GameObject("SpeakerTarget").SetParentAndZero(_LocalTransform).transform;
            }

            SpeakerTransform = SpeakerTarget;

            //if (!_SpeakerTarget.TryGetComponent(out _SpeakerAttachmentLine))
            //    _SpeakerAttachmentLine = _SpeakerTarget.gameObject.AddComponent<AttachmentLine>();
            //{
            //    _SpeakerAttachmentLine._TransformA = _SpeakerTarget;
            //}
        }

        private void InitialiseSurfaceProperties()
        {
            if (TryGetComponent(out _SurfaceProperties) || _LocalTransform.gameObject.TryGetComponent(out _SurfaceProperties))
            {
                _SelfRigidity = _SurfaceProperties.Rigidity;
            }
            else
            {
                _SurfaceProperties = _LocalTransform.gameObject.AddComponent<SurfaceProperties>();
                _SurfaceProperties.Rigidity = _SelfRigidity;
            }
        }

        public void AssignControllers(ObjectSpawner objectSpawner, SpawnableManager spawnableManager, Transform local, Transform remote)
        {
            _Spawner = objectSpawner;
            _SpawnLife = spawnableManager;
            _LocalTransform = local;
            _RemoteTransform = remote;

            foreach (BehaviourClass behaviour in GetComponents<BehaviourClass>())
            {
                behaviour._SpawnedObject = _LocalTransform.gameObject;
                behaviour._ControllerObject = _RemoteTransform.gameObject;
                behaviour._ObjectSpawner = _Spawner;
            }

            _RemotelyAssigned = true;
        }

        #endregion

        #region HEFTY HOST COMPONENT BUSINESS

        protected override void SetElementType()
        {
            ElementType = SynthElementType.Host;
        }

        protected override void InitialiseComponents()
        {
            Manager.SetComponentData(ElementEntity, new HostComponent
            {
                HostIndex = EntityIndex,
                Connected = false,
                SpeakerIndex = int.MaxValue,
                InListenerRadius = InListenerRadius,
                WorldPos = SpeakerTarget.position
            });
        }

        protected override void ProcessComponents()
        {
            HostComponent hostData = Manager.GetComponentData<HostComponent>(ElementEntity);
            hostData.HostIndex = EntityIndex;
            hostData.WorldPos = SpeakerTarget.position;
            Manager.SetComponentData(ElementEntity, hostData);

            bool connected = GrainBrain.Instance.IsSpeakerAtIndex(hostData.SpeakerIndex, out SpeakerAuthoring speaker);
            _InListenerRadius = hostData.InListenerRadius;
            SpeakerTransform = connected ? speaker.gameObject.transform : SpeakerTarget;
            _AttachedSpeakerIndex = connected ? hostData.SpeakerIndex : int.MaxValue;
            
            
            MaterialModulator.SetActiveState(connected);
            MaterialModulator.Tick();

            _Connected = connected;

            ProcessRigidity();

            if (!connected)
                return;

            float speakerAmplitudeFactor = ScaleAmplitude.SpeakerOffsetFactor(
                transform.position,
                _headTransform.position,
                SpeakerTransform.position);
            
            _ListenerDistance = Vector3.Distance(SpeakerTarget.position, _headTransform.position);

            foreach (EmitterAuthoring emitter in _HostedEmitters)
            {
                emitter.UpdateDistanceAmplitude(_ListenerDistance / GrainBrain.Instance._ListenerRadius, speakerAmplitudeFactor);
            }
        }

        protected override void Deregister()
        {
            if (GrainBrain.Instance != null)
                GrainBrain.Instance.DeregisterHost(this);
        }

        #endregion

        #region BEHAVIOUR UPDATES

        public void ProcessRigidity()
        {
            // Clear lingering null contact objects and find most rigid collider value
            _CollidingObjects.RemoveAll(item => item == null);
            _TargetCollidingRigidity = 0;

            foreach (GameObject go in _CollidingObjects)
            {
                if (go.TryGetComponent(out SurfaceProperties props))
                {
                    _TargetCollidingRigidity = _TargetCollidingRigidity > props.Rigidity ? _TargetCollidingRigidity : props.Rigidity;
                }
            }
            // Smooth transition to upward rigidity values to avoid randomly triggering surface contact emitters from short collisions
            if (_TargetCollidingRigidity < CollidingRigidity + 0.001f || _EaseCollidingRigidity <= 0)
                _CollidingRigidity = _TargetCollidingRigidity;
            else
                _CollidingRigidity = CollidingRigidity.Lerp(_TargetCollidingRigidity, RigiditySmoothUp * Time.deltaTime);
            if (CollidingRigidity < 0.001f) _CollidingRigidity = 0;
        }

        //public void UpdateSpeakerAttachmentLine()
        //{
        //    if (_SpeakerAttachmentLine == null)
        //        return;

        //    if (_SpeakerTransform != _SpeakerTarget)
        //        _SpeakerAttachmentLine._TransformB = _SpeakerTransform;

        //    _SpeakerAttachmentLine._Active = _Connected && GrainBrain.Instance._DrawAttachmentLines;
        //}

        #endregion

        #region COLLISION HANDLING

        // public void OnCollisionEnter(Collision collision)
        // {
        //     GameObject other = collision.collider.gameObject;
        //     _CollidingObjects.Add(other);
        //
        //     if (ContactAllowed(other)) _LocalActor.LatestCollision = collision;
        //
        //     if (_Spawner == null || _Spawner.CollisionAllowed(_LocalTransform.gameObject, other))
        //     {
        //         foreach (EmitterAuthoring emitter in _HostedEmitters)
        //             emitter.NewCollision(collision);
        //     }
        // }
        //
        // public void OnCollisionStay(Collision collision)
        // {
        //     Collider collider = collision.collider;
        //     _IsColliding = true;
        //
        //     if (ContactAllowed(collider.gameObject))
        //     {
        //         _LocalActor.IsColliding = true;
        //
        //         foreach (EmitterAuthoring emitter in _HostedEmitters)
        //             emitter.UpdateContactStatus(collision);
        //     }
        // }
        //
        // public void OnCollisionExit(Collision collision)
        // {
        //     _CollidingObjects.Remove(collision.collider.gameObject);
        //
        //     if (_CollidingObjects.Count == 0)
        //     {
        //         _IsColliding = false;
        //         _LocalActor.IsColliding = false;
        //         _TargetCollidingRigidity = 0;
        //         _CollidingRigidity = 0;
        //         foreach (EmitterAuthoring emitter in _HostedEmitters)
        //             emitter.UpdateContactStatus(null);
        //     }
        // }
        //
        // public bool ContactAllowed(GameObject other)
        // {
        //     return _Spawner == null || _Spawner.ContactAllowed(gameObject, other);
        // }

        #endregion


        private void OnDrawGizmos()
        {
            if (!_Connected || SpeakerTransform == SpeakerTarget)
                return;

            Gizmos.DrawLine(SpeakerTarget.position, SpeakerTransform.position);
            Gizmos.DrawWireSphere(SpeakerTarget.position, .1f);
        }
    }
}
