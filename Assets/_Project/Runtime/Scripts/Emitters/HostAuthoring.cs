using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

using MaxVRAM.Audio;
using MaxVRAM.Extensions;
using NaughtyAttributes;

namespace PlaneWaver
{
    /// <summary>
    //      Multiple emitters are often spawned and attached to the same object and modulated by
    //      the same game-world interactions. Emitter Hosts manage emitters designed to create
    //      a single "profile" for an interactive sound object.
    /// <summary>
    public class HostAuthoring : SynthEntity
    {
        #region FIELDS & PROPERTIES

        private Transform _HeadTransform;

        [AllowNesting]
        [BoxGroup("Speaker Attachment")]
        [Tooltip("Parent transform position for speakers to target for this host. Defaults to this transform.")]
        [SerializeField] private Transform _SpeakerTarget;
        [AllowNesting]
        [BoxGroup("Speaker Attachment")]
        [SerializeField] private Transform _SpeakerTransform;
        [AllowNesting]
        [BoxGroup("Speaker Attachment")]
        public MaterialColourModulator _MaterialModulator = new();

        private AttachmentLine _AttachmentLine;

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

            _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _Archetype = _EntityManager.CreateArchetype(typeof(HostComponent));
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

            _LocalActor = new(_LocalTransform);
            _RemoteActor = _RemoteTransform != null ? new(_RemoteTransform) : null;

            _SpeakerTarget = _SpeakerTarget != null ? _SpeakerTarget : _LocalTransform;
            _SpeakerTransform = _SpeakerTarget;

            if (_AttachmentLine = TryGetComponent(out _AttachmentLine) ? _AttachmentLine : gameObject.AddComponent<AttachmentLine>())
                _AttachmentLine._TransformA = _SpeakerTarget;

            if (TryGetComponent(out _SurfaceProperties) || _LocalTransform.gameObject.TryGetComponent(out _SurfaceProperties))
            {
                _SelfRigidity = _SurfaceProperties._Rigidity;
            }
            else
            {
                _SurfaceProperties = _LocalTransform.gameObject.AddComponent<SurfaceProperties>();
                _SurfaceProperties._Rigidity = _SelfRigidity;
            }

            if (_MaterialModulator._Renderer == null)
                _MaterialModulator._Renderer = _LocalTransform.GetComponentInChildren<Renderer>();

            _HeadTransform = FindObjectOfType<Camera>().transform;

            _HostedEmitters.AddRange(_LocalTransform.GetComponentsInChildren<EmitterAuthoring>());

            foreach (EmitterAuthoring emitter in _HostedEmitters)
                emitter.InitialiseByHost(this);
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

        public override void SetEntityType()
        {
            _EntityType = SynthEntityType.Host;
        }

        public override void InitialiseComponents()
        {
            _EntityManager.SetComponentData(_Entity, new HostComponent
            {
                _HostIndex = _EntityIndex,
                _Connected = false,
                _SpeakerIndex = int.MaxValue,
                _InListenerRadius = InListenerRadius,
                _WorldPos = _SpeakerTarget.position
            });
        }

        public override void ProcessComponents()
        {
            HostComponent hostData = _EntityManager.GetComponentData<HostComponent>(_Entity);
            hostData._HostIndex = _EntityIndex;
            hostData._WorldPos = _SpeakerTarget.position;
            _EntityManager.SetComponentData(_Entity, hostData);

            bool connected = GrainBrain.Instance.IsSpeakerAtIndex(hostData._SpeakerIndex, out SpeakerAuthoring speaker);
            _InListenerRadius = hostData._InListenerRadius;
            _SpeakerTransform = connected ? speaker.gameObject.transform : _SpeakerTarget;
            _AttachedSpeakerIndex = connected ? hostData._SpeakerIndex : int.MaxValue;
            
            if (_Connected != connected)
                _MaterialModulator.SetActiveState(connected);

            _MaterialModulator.Tick();

            _Connected = connected;

            UpdateSpeakerAttachmentLine();
            ProcessRigidity();

            float speakerAmplitudeFactor = ScaleAmplitude.SpeakerOffsetFactor(
                transform.position,
                _HeadTransform.position,
                _SpeakerTransform.position);
            
            _ListenerDistance = Vector3.Distance(_SpeakerTarget.position, _HeadTransform.position);

            foreach (EmitterAuthoring emitter in _HostedEmitters)
            {
                emitter.UpdateDistanceAmplitude(_ListenerDistance / GrainBrain.Instance._ListenerRadius, speakerAmplitudeFactor);
            }
        }

        public override void Deregister()
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
                    _TargetCollidingRigidity = _TargetCollidingRigidity > props._Rigidity ? _TargetCollidingRigidity : props._Rigidity;
                }
            }
            // Smooth transition to upward rigidity values to avoid randomly triggering surface contact emitters from short collisions
            if (_TargetCollidingRigidity < CollidingRigidity + 0.001f || _EaseCollidingRigidity <= 0)
                _CollidingRigidity = _TargetCollidingRigidity;
            else
                _CollidingRigidity = CollidingRigidity.Lerp(_TargetCollidingRigidity, RigiditySmoothUp * Time.deltaTime);
            if (CollidingRigidity < 0.001f) _CollidingRigidity = 0;
        }

        public void UpdateSpeakerAttachmentLine()
        {
            if (_AttachmentLine == null)
                return;

            if (!_AttachmentLine.isActiveAndEnabled)
            {
                _AttachmentLine.enabled = true;
            }
            if (_Connected && _SpeakerTransform != _SpeakerTarget && GrainBrain.Instance._DrawAttachmentLines)
            {
                _AttachmentLine._Active = true;
                _AttachmentLine._TransformB = _SpeakerTransform;
            }
            else
                _AttachmentLine._Active = false;
        }

        #endregion

        #region COLLISION HANDLING

        public void OnCollisionEnter(Collision collision)
        {
            GameObject other = collision.collider.gameObject;
            _CollidingObjects.Add(other);

            if (ContactAllowed(other)) _LocalActor.LatestCollision = collision;

            if (_Spawner == null || _Spawner.UniqueCollision(_LocalTransform.gameObject, other))
                foreach (EmitterAuthoring emitter in _HostedEmitters)
                    emitter.NewCollision(collision);
        }

        public void OnCollisionStay(Collision collision)
        {
            Collider collider = collision.collider;
            _IsColliding = true;

            if (ContactAllowed(collider.gameObject))
            {
                _LocalActor.IsColliding = true;

                foreach (EmitterAuthoring emitter in _HostedEmitters)
                    emitter.UpdateContactStatus(collision);
            }
        }

        public void OnCollisionExit(Collision collision)
        {
            _CollidingObjects.Remove(collision.collider.gameObject);

            if (_CollidingObjects.Count == 0)
            {
                _IsColliding = false;
                _LocalActor.IsColliding = false;
                _TargetCollidingRigidity = 0;
                _CollidingRigidity = 0;
                foreach (EmitterAuthoring emitter in _HostedEmitters)
                    emitter.UpdateContactStatus(null);
            }
        }

        public bool ContactAllowed(GameObject other)
        {
            return _Spawner == null || _Spawner.IsContactAllowed(gameObject, other);
        }

        #endregion
    }
}
