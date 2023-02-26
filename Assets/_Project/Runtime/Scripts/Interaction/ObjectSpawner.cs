using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System;

using NaughtyAttributes;

using MaxVRAM;
using MaxVRAM.Ticker;
using MaxVRAM.Extensions;

namespace PlaneWaver
{
    /// <summary>
    /// Manager for spawning child game objects with a variety of existance and controller behaviours.
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        #region FIELDS & PROPERTIES

        public enum SpawnCondition { Never, AfterSpeakersPopulated, IfSpeakerAvailable, AfterDelayPeriod, Always }
        public enum ControllerEvent { Off, OnSpawn, OnCollision, All };
        public enum SiblingCollision { All, Single, None };

        [Header("Runtime Dynamics")]
        [SerializeField] private bool _Initialised = false;
        [SerializeField] private int _ObjectCounter = 0;
        public HashSet<GameObject> _CollidedThisUpdate;

        [Header("Object Configuration")]
        [Tooltip("Object providing the spawn location and controller behaviour.")]
        public GameObject _ControllerObject;
        public Transform _ControllerAnchor;
        [Tooltip("Parent GameObject to attach spawned prefabs.")]
        public GameObject _SpawnableHost;
        [Tooltip("Prefab to spawn.")]
        public GameObject _PrefabToSpawn;
        [Tooltip("A list of prebs to spawn can also be supplied, allowing runtime selection of object spawn selection.")]
        public List<GameObject> _SpawnablePrefabs;
        public bool _RandomiseSpawnPrefab = false;
        public BaseJointScriptable _AttachmentJoint;

        [Header("Spawning Rules")]
        [SerializeField][Range(0, 100)] private int _SpawnablesAllocated = 10;
        [Tooltip("Delay (seconds) between each spawn.")]
        [Range(0.01f, 1)][SerializeField] private float _SpawnPeriodSeconds = 0.2f;
        [SerializeField] private SpawnCondition _SpawnCondition = SpawnCondition.AfterSpeakersPopulated;
        public bool SpawnAfterPeriodSet => _SpawnCondition == SpawnCondition.AfterDelayPeriod;
        [EnableIf("SpawnAfterPeriodSet")]
        [Tooltip("Number of seconds after this ObjectSpawner is created before it starts spawning loop.")]
        [SerializeField] private float _SpawnDelaySeconds = 2;
        [SerializeField] private bool _AutoSpawn = true;
        [SerializeField] private bool _AutoRemove = true;
        private float _StartTime = int.MaxValue;
        private bool _StartTimeReached = false;
        private Trigger _SpawnTimer;

        [Header("Object Properties")]
        [SerializeField]
        [MinMaxSlider(0.01f, 2)]
        private Vector2 _SpawnObjectScale = new(1, 1);

        [Header("Spawn Position")]
        [Tooltip("Spawn position relative to the controller object. Converts to unit vector.")]
        [MinValue(-1)][MaxValue(1)]
        public Vector3 _EjectionPosition = new(1, 0, 0);
        [Tooltip("Apply randomisation to the spawn position unit vector.")]
        [Range(0f, 1f)] public float _PositionVariance = 0;
        [Tooltip("Distance from the controller to spawn objects.")]
        [MinMaxSlider(0f, 10f)] public Vector2 _EjectionRadius = new(1, 2);

        [Header("Spawn Velocity")]
        [MinValue(-1)][MaxValue(1)]
        public Vector3 _EjectionDirection = new(0, 0, 1);
        public Vector3 EjectionDirection => Vector3.Scale(_EjectionDirection, new Vector3(1,1,-1));
        [Tooltip("Apply randomisation to the spawn direction.")]
        [Range(0f, 1f)] public float _DirectionVariance = 0;
        [Tooltip("Speed that spawned objects leave the controller.")]
        [MinMaxSlider(0f, 20)] public Vector2 _EjectionSpeed = new(0, 2);
        
        [Header("Object Removal")]
        public bool _DestroyOnAllCollisions = false;
        [Tooltip("Coodinates that define the bounding for spawned objects, which are destroyed if they leave. The bounding radius is ignored when using Collider Bounds, defined instead by the supplied collider bounding area, deaulting to the controller's collider if it has one.")]
        public BoundingArea _BoundingAreaType = BoundingArea.ControllerTransform;
        [EnableIf("UsingColliderBounds")] public Collider _BoundingCollider;
        [Tooltip("Radius of the bounding volume.")]
        [EnableIf("UsingBoundingRadius")] public float _BoundingRadius = 30f;
        [Tooltip("Use a timer to destroy spawned objects after a duration.")]
        [SerializeField] private bool _UseSpawnDuration = true;
        [Tooltip("Duration in seconds before destroying spawned object.")]
        [EnableIf("_UseSpawnDuration")]
        [MinMaxSlider(0f, 60f)] public Vector2 _SpawnObjectDuration = new(5, 10);
        public bool UsingBoundingRadius => _BoundingAreaType is BoundingArea.SpawnPosition or BoundingArea.ControllerTransform;
        public bool UsingColliderBounds => _BoundingAreaType is BoundingArea.ColliderBounds;

        [Header("Emitter Behaviour")]
        public bool _AllowSiblingSurfaceContact = true;
        public SiblingCollision _AllowSiblingCollisionTrigger = SiblingCollision.Single;

        [Header("Visual Feedback")]
        [SerializeField] private ControllerEvent _EmissiveFlashTrigger = ControllerEvent.OnSpawn;
        public MaterialColourModulator _MaterialModulator;

        private List<GameObject> _ActiveObjects = new();

        #endregion

        #region INITIALISATION

        void Start()
        {
            if (!InitialiseSpawner())
                gameObject.SetActive(false);
        }

        void Awake()
        {
            StartCoroutine(ClearCollisions());
            _SpawnTimer = new Trigger(TimeUnit.seconds, _SpawnPeriodSeconds);
        }

        IEnumerator ClearCollisions()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                _CollidedThisUpdate.Clear();
            }
        }

        private bool InitialiseSpawner()
        {
            _CollidedThisUpdate = new HashSet<GameObject>();

            if (_ControllerObject == null)
                _ControllerObject = gameObject;

            if (_SpawnableHost == null)
                _SpawnableHost = gameObject;

            if (_SpawnablePrefabs.Count == 0 && _PrefabToSpawn != null)
                _SpawnablePrefabs.Add(_PrefabToSpawn);

            if (_SpawnablePrefabs.Count >= 1 && _PrefabToSpawn == null)
                _PrefabToSpawn = _SpawnablePrefabs[0];

            if (_SpawnablePrefabs.Count == 0)
            {
                Debug.LogWarning($"{name} not assigned any prefabs!");
                _Initialised = false;
                return false;
            }

            if (_ControllerAnchor != null && _ControllerObject.TryGetComponent(out Rigidbody rb)
                && _ControllerAnchor.TryGetComponent(out SpringJoint joint))
            {
                joint.connectedBody = rb;
            }

            _Initialised = true;
            _StartTime = Time.time + _SpawnDelaySeconds;
            return true;
        }

        #endregion

        #region UPDATE SCHEDULE

        void Update()
        {
            _ActiveObjects.RemoveAll(item => item == null);
            _SpawnTimer.UpdateTrigger(Time.deltaTime, _SpawnPeriodSeconds);

            if (!ReadyToSpawn())
                return;

            if (_AutoSpawn)
                CreateSpawnable();
            else if (_AutoRemove)
                RemoveSpawnable(0);
        }

        #endregion

        #region SPAWN MANAGEMENT

        public bool ReadyToSpawn()
        {
            if (!_Initialised)
                return false;
            if ((_ControllerObject == null | _PrefabToSpawn == null) && !InitialiseSpawner())
                return false;
            return SpawningAllowed();
        }

        public bool SpawningAllowed()
        {
            return _SpawnCondition switch
            {
                SpawnCondition.Never => false,
                SpawnCondition.Always => true,
                SpawnCondition.AfterSpeakersPopulated => !GrainBrain.Instance.PopulatingSpeakers,
                SpawnCondition.IfSpeakerAvailable => !GrainBrain.Instance._Speakers.TrueForAll(s => s.IsActive),
                SpawnCondition.AfterDelayPeriod => _StartTimeReached || (_StartTimeReached = Time.time > _StartTime),
                _ => false,
            };
        }

        public void CreateSpawnable()
        {
            if (_ActiveObjects.Count >= _SpawnablesAllocated || !_SpawnTimer.DrainTrigger())
                return;

            InstantiatePrefab(out GameObject newObject);
            ConfigureSpawnedObject(newObject, _ControllerObject);
            newObject.SetActive(true);
            _ActiveObjects.Add(newObject);

            if (_EmissiveFlashTrigger == ControllerEvent.OnSpawn)
                _MaterialModulator.Flash();
        }

        public void RemoveSpawnable(int index)
        {
            if (_ActiveObjects.Count <= _SpawnablesAllocated || !_SpawnTimer.DrainTrigger())
                return;
            if (_ActiveObjects[index] != null)
                Destroy(_ActiveObjects[index]);
            _ActiveObjects.RemoveAt(index);

        }

        public bool InstantiatePrefab(out GameObject newObject)
        {
            int index = (!_RandomiseSpawnPrefab || _SpawnablePrefabs.Count < 2) ? Random.Range(0, _SpawnablePrefabs.Count) : 0;
            GameObject objectToSpawn = _SpawnablePrefabs[index];

            float spawnDistance = Rando.Range(_EjectionRadius);
            Vector3 randomPosition = Random.onUnitSphere;
            Vector3 spawnUnitPosition = Vector3.Slerp(_EjectionPosition.normalized, randomPosition, _PositionVariance);
            Vector3 spawnPositionLocal = spawnUnitPosition * spawnDistance;
            Vector3 spawnPositionWorld = _ControllerObject.transform.position + spawnPositionLocal;
            Quaternion lookAtSpawner = Quaternion.LookRotation(Vector3.zero - spawnUnitPosition);

            newObject = Instantiate(objectToSpawn, spawnPositionWorld, lookAtSpawner, _SpawnableHost.transform);
            newObject.transform.localScale *= Rando.Range(_SpawnObjectScale);
            newObject.name = newObject.name + " (" + _ObjectCounter + ")";

            if (!newObject.TryGetComponent(out Rigidbody rb))
                rb = newObject.AddComponent<Rigidbody>();

            Vector3 randomDirection = Random.onUnitSphere;
            Vector3 spawnUnitDirection = Vector3.Slerp(EjectionDirection.normalized, randomDirection, _DirectionVariance);

            Vector3 velocity = newObject.transform.localRotation * spawnUnitDirection * EjectionDirection.magnitude * Rando.Range(_EjectionSpeed);
            rb.velocity = velocity;

            return true;
        }

        public SpawnableManager AttachSpawnableManager(GameObject go)
        {
            if (!go.TryGetComponent(out SpawnableManager spawnableManager))
                spawnableManager = go.AddComponent<SpawnableManager>();

            spawnableManager._Lifespan = _UseSpawnDuration ? Rando.Range(_SpawnObjectDuration) : int.MaxValue;
            spawnableManager._BoundingAreaType = _BoundingAreaType;
            spawnableManager._BoundingCollider = _BoundingCollider;
            spawnableManager._ControllerObject = _ControllerObject;
            spawnableManager._BoundingRadius = _BoundingRadius;
            spawnableManager._ObjectSpawner = this;
            spawnableManager._SpawnedObject = go;
            return spawnableManager;
        }

        // TODO: Decouple Synthesis authoring from this
        public void ConfigureSpawnedObject(GameObject spawned, GameObject controller)
        {
            SpawnableManager spawnableManager = AttachSpawnableManager(spawned);

            if (_AttachmentJoint != null)
                spawned.AddComponent<InteractionJointController>().Initialise(_AttachmentJoint, controller.transform);

            if (!spawned.TryGetComponent(out HostAuthoring newHost))
                newHost = spawned.GetComponentInChildren(typeof(HostAuthoring), true) as HostAuthoring;

            if (newHost == null)
                return;

            newHost.AssignControllers(this, spawnableManager, spawned.transform, controller.transform);
        }
        #endregion

        #region RUNTIME MODULATIONS

        public bool UniqueCollision(GameObject goA, GameObject goB)
        {
            if (!_ActiveObjects.Contains(goA) || !_ActiveObjects.Contains(goB) || _AllowSiblingCollisionTrigger == SiblingCollision.All)
                return true;
            else if (_AllowSiblingCollisionTrigger == SiblingCollision.None)
                return false;
            else if (_AllowSiblingCollisionTrigger == SiblingCollision.Single & _CollidedThisUpdate.Add(goA) | _CollidedThisUpdate.Add(goB))
                return true;
            else
                return false;
        }

        public bool IsContactAllowed(GameObject goA, GameObject goB)
        {
            return !_ActiveObjects.Contains(goA) || _AllowSiblingSurfaceContact || !_ActiveObjects.Contains(goB);
        }

        #endregion
    }
}