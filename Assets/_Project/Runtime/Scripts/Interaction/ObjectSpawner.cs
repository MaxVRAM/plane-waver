using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System;

using NaughtyAttributes;

using MaxVRAM;
using MaxVRAM.Ticker;

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

        [Header("Ejection Physics")]
        [Tooltip("Direction that determines a spawnables position and velocity on instantiation. Converts to unit vector.")]
        public Vector3 _EjectionDirection = new(0, 0, 0);
        [Tooltip("Direction to rotate the exit velocity from its inital direction (enables initiating a velocity spinning around the spawner). Converts to unit vector.")]
        [MinValue(-1)][MaxValue(1)] public Vector3 _VelocityUpOffset = new(0, 1, 0);
        [Tooltip("Amount of random spread applied to each spawn direction.")]
        [Range(0f, 1f)] public float _EjectionDirectionVariance = 0;
        [Tooltip("Distance from the anchor that objects will be spawned.")]
        [MinMaxSlider(0f, 10f)] public Vector2 _EjectionRadiusRange = new(1, 2);
        [Tooltip("Speed that spawned objects leave the anchor.")]
        [MinMaxSlider(0f, 100f)] public Vector2 _EjectionSpeedRange = new(5, 10);
        
        [Header("Spawned Object Removal")]
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

            Vector3 randomDirection = Random.onUnitSphere;
            Vector3 spawnDirection = Vector3.Slerp(_EjectionDirection.normalized, randomDirection, _EjectionDirectionVariance);
            Vector3 spawnPosition = _ControllerObject.transform.position + spawnDirection * Rando.Range(_EjectionRadiusRange);
            Quaternion directionRotation = Quaternion.FromToRotation(_VelocityUpOffset, spawnDirection);

            newObject = Instantiate(objectToSpawn, spawnPosition, directionRotation, _SpawnableHost.transform);
            newObject.name = newObject.name + " (" + _ObjectCounter + ")";

            if (!newObject.TryGetComponent(out Rigidbody rb)) rb = newObject.AddComponent<Rigidbody>();
            rb.velocity = spawnDirection * Rando.Range(_EjectionSpeedRange);

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
            if (!spawned.TryGetComponent(out HostAuthoring newHost))
                newHost = spawned.GetComponentInChildren(typeof(HostAuthoring), true) as HostAuthoring;

            if (newHost == null)
                return;

            newHost.AssignControllers(this, AttachSpawnableManager(spawned), spawned.transform, controller.transform);

            if (_AttachmentJoint != null)
                spawned.AddComponent<InteractionJointController>().Initialise(_AttachmentJoint, controller.transform);
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