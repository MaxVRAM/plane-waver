using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Linq;
using NaughtyAttributes;

using MaxVRAM;
using MaxVRAM.Ticker;

namespace PlaneWaver
{
    /// <summary>
    /// Manager for spawning child game objects with a variety of existence and controller behaviours.
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        #region CLASS DEFINITIONS

        public enum SpawnCondition { Never, AfterSpeakersPopulated, SpeakerAvailable, AfterDelayPeriod, Always }
        public enum ControllerEvent { Off, OnSpawn, OnCollision, All };
        public enum SiblingCollision { All, Single, None };

        [Header("Runtime Dynamics")]
        private bool _initialised;
        private HashSet<GameObject> _collidedThisUpdate;

        [Header("Object Configuration")]
        [Tooltip("Object providing the spawn location and controller behaviour.")]
        public GameObject ControllerObject;
        public Transform ControllerAnchor;
        [Tooltip("Parent GameObject to attach spawned prefabs.")]
        public GameObject SpawnableHost;
        [Tooltip("Prefab to spawn.")]
        public GameObject PrefabToSpawn;
        [Tooltip("A list of prefabs to spawn can also be supplied, allowing runtime selection of object spawn selection.")]
        public List<GameObject> SpawnablePrefabs;
        public bool RandomiseSpawnPrefab;
        public BaseJointScriptable AttachmentJoint;

        [Header("Spawning Rules")]
        [Range(0, 100)]
        public int MaxSpawnedObjects = 10;
        [Tooltip("Delay (seconds) between each spawn.")]
        [Range(0.01f, 1)]
        public float SpawnPeriodSeconds = 0.2f;
        public SpawnCondition SpawnWhen = SpawnCondition.AfterSpeakersPopulated;
        public bool SpawnAfterPeriodSet => SpawnWhen == SpawnCondition.AfterDelayPeriod;
        [EnableIf("SpawnAfterPeriodSet")]
        [Tooltip("Number of seconds after this ObjectSpawner is created before it starts spawning loop.")]
        public float SpawnDelaySeconds = 2;
        public bool AutoSpawn = true;
        public bool AutoRemove = true;
        
        private float _startTime = int.MaxValue;
        private bool _startTimeReached;
        private Trigger _spawnTimer;

        [Header("Object Properties")]
        [SerializeField]
        [MinMaxSlider(0.01f, 2)]
        public Vector2 SpawnObjectScale = new(1, 1);

        [Header("Spawn Position")]
        [Tooltip("Spawn position relative to the controller object. Converts to unit vector.")]
        [MinValue(-1)][MaxValue(1)]
        public Vector3 EjectionPosition = new(1, 0, 0);
        [Tooltip("Apply randomisation to the spawn position unit vector.")]
        [Range(0f, 1f)]
        public float PositionVariance = 0;
        [Tooltip("Distance from the controller to spawn objects.")]
        [MinMaxSlider(0f, 10f)]
        public Vector2 EjectionRadius = new(1, 2);

        [Header("Spawn Velocity")]
        [MinValue(-1)][MaxValue(1)]
        public Vector3 EjectionDirection = new(0, 0, 1);
        [Tooltip("Apply randomisation to the spawn direction.")]
        [Range(0f, 1f)]
        public float DirectionVariance = 0;
        [Tooltip("Speed that spawned objects leave the controller.")]
        [MinMaxSlider(0f, 20)]
        public Vector2 EjectionSpeed = new(0, 2);
        
        [Header("Object Removal")]
        [Tooltip("Coordinates that define the bounding for spawned objects, which are destroyed if they leave. The bounding radius is ignored when using Collider Bounds, defined instead by the supplied collider bounding area, deaulting to the controller's collider if it has one.")]
        public ActorBounds BoundingAreaType = ActorBounds.ControllerTransform;
        [EnableIf("UsingColliderBounds")]
        public Collider BoundingCollider;
        [Tooltip("Radius of the bounding volume.")]
        [EnableIf("UsingBoundingRadius")]
        public float BoundingRadius = 30f;
        [Tooltip("Use a timer to destroy spawned objects after a duration.")]
        public bool UseSpawnLifespan = true;
        [Tooltip("Duration in seconds before destroying spawned object.")]
        [EnableIf("_UseSpawnDuration")]
        [MinMaxSlider(0f, 60f)] public Vector2 SpawnLifeSpanDuration = new(5, 10);
        private float SpawnLifespan => UseSpawnLifespan ? Rando.Range(SpawnLifeSpanDuration) : -1;
        public bool UsingBoundingRadius => BoundingAreaType is ActorBounds.SpawnPosition or ActorBounds.ControllerTransform;
        public bool UsingColliderBounds => BoundingAreaType is ActorBounds.ColliderBounds;

        [Header("Emitter Behaviour")]
        public bool AllowSiblingSurfaceContact = true;
        public SiblingCollision AllowSiblingCollisionTrigger = SiblingCollision.Single;

        [Header("Visual Feedback")]
        public ControllerEvent EmissiveFlashTrigger = ControllerEvent.OnSpawn;
        public MaterialColourModulator MaterialModulator;

        private List<GameObject> _activeObjects;

        #endregion

        #region INITIALISATION METHODS

        private void Start()
        {
            if (!InitialiseSpawner())
                gameObject.SetActive(false);
        }

        private void Awake()
        {
            StartCoroutine(ClearCollisions());
            _spawnTimer = new Trigger(TimeUnit.seconds, SpawnPeriodSeconds);
            _activeObjects = new List<GameObject>();
        }

        IEnumerator ClearCollisions()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                _collidedThisUpdate.Clear();
            }
        }

        private bool InitialiseSpawner()
        {
            _collidedThisUpdate = new HashSet<GameObject>();

            if (ControllerObject == null)
                ControllerObject = gameObject;

            if (SpawnableHost == null)
                SpawnableHost = gameObject;

            if (SpawnablePrefabs.Count == 0 && PrefabToSpawn != null)
                SpawnablePrefabs.Add(PrefabToSpawn);

            if (SpawnablePrefabs.Count >= 1 && PrefabToSpawn == null)
                PrefabToSpawn = SpawnablePrefabs[0];

            if (SpawnablePrefabs.Count == 0)
            {
                Debug.LogWarning($"{name} not assigned any prefabs!");
                _initialised = false;
                return false;
            }

            if (ControllerAnchor != null && ControllerObject.TryGetComponent(out Rigidbody rb)
                && ControllerAnchor.TryGetComponent(out SpringJoint joint))
            {
                joint.connectedBody = rb;
            }

            _initialised = true;
            _startTime = Time.time + SpawnDelaySeconds;
            return true;
        }

        #endregion

        #region UPDATE METHODS

        void Update()
        {
            _activeObjects.RemoveAll(item => item == null);
            _spawnTimer.UpdateTrigger(Time.deltaTime, SpawnPeriodSeconds);

            if (!ReadyToSpawn())
                return;

            if (AutoSpawn)
                CreateSpawnable();
            else if (AutoRemove)
                RemoveSpawnable(0);
        }

        #endregion

        #region SPAWNING MANAGEMENT

        private bool ReadyToSpawn()
        {
            return _initialised && SpawningAllowed();
        }

        private bool SpawningAllowed()
        {
            return SpawnWhen switch
            {
                SpawnCondition.Never => false,
                SpawnCondition.Always => true,
                SpawnCondition.AfterSpeakersPopulated => !GrainBrain.Instance.PopulatingSpeakers,
                SpawnCondition.SpeakerAvailable => GrainBrain.Instance._Speakers
                                                             .FirstOrDefault(s => !s.IsActive),
                SpawnCondition.AfterDelayPeriod => _startTimeReached || (_startTimeReached = Time.time > _startTime),
                _ => false,
            };
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void CreateSpawnable()
        {
            if (_activeObjects.Count >= MaxSpawnedObjects || !_spawnTimer.DrainTrigger())
                return;

            InstantiatePrefab(out GameObject newObject);
            ConfigureSpawnedObject(newObject, ControllerObject);
            newObject.SetActive(true);
            _activeObjects.Add(newObject);

            if (EmissiveFlashTrigger == ControllerEvent.OnSpawn)
                MaterialModulator.Flash();
        }

        private void RemoveSpawnable(int index)
        {
            if (_activeObjects.Count <= MaxSpawnedObjects || !_spawnTimer.DrainTrigger())
                return;
            if (index < _activeObjects.Count)
                Destroy(_activeObjects[index]);
            _activeObjects.RemoveAt(index);
        }
        
        #endregion

        #region SPAWN CONFIGURATION
        
        private void InstantiatePrefab(out GameObject newObject)
        {
            int index = (!RandomiseSpawnPrefab || SpawnablePrefabs.Count < 2) ? Random.Range(0, SpawnablePrefabs.Count) : 0;
            GameObject objectToSpawn = SpawnablePrefabs[index];

            GenerateSpawnTranslation(out Vector3 spawnPosition, out Quaternion spawnRotation);
            newObject = Instantiate(objectToSpawn, spawnPosition, spawnRotation, SpawnableHost.transform);
            newObject.transform.localScale *= Rando.Range(SpawnObjectScale);
            ApplySpawnVelocity(newObject);
        }

        private void GenerateSpawnTranslation(out Vector3 spawnPosition, out Quaternion spawnRotation)
        {
            float spawnDistance = Rando.Range(EjectionRadius);
            Vector3 randomPosition = Random.onUnitSphere;
            Vector3 unitPosition = Vector3.Slerp(EjectionPosition.normalized, randomPosition, PositionVariance);
            Vector3 positionLocal = unitPosition * spawnDistance;
            spawnPosition = ControllerObject.transform.position + positionLocal;
            spawnRotation = Quaternion.LookRotation(unitPosition);
        }

        private void ApplySpawnVelocity(GameObject newObject)
        {
            Rigidbody rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            Vector3 randomDirection = Random.onUnitSphere;
            Vector3 spawnUnitDirection = Vector3.Slerp(EjectionDirection.normalized, randomDirection, DirectionVariance);
            Vector3 velocity = newObject.transform.localRotation * spawnUnitDirection * EjectionDirection.magnitude *
                               Rando.Range(EjectionSpeed);
            rb.velocity = velocity;
        }

        private void ConfigureSpawnedObject(GameObject spawnedObject, GameObject controllerObject)
        {
            if (AttachmentJoint != null)
                spawnedObject.AddComponent<InteractionJointController>()
                             .Initialise(AttachmentJoint, controllerObject.transform);

            Actor actor = GetComponent<Actor>() ?? spawnedObject.AddComponent<Actor>();
            actor.OtherBody = controllerObject.transform;
            actor.ActorLifeData = new ActorLifeData(
                SpawnLifespan,
                BoundingRadius,
                BoundingAreaType,
                BoundingCollider,
                controllerObject.transform);
        }
        
        #endregion

        #region COLLISION MANAGEMENT

        public bool CollisionAllowed(GameObject goA, GameObject goB)
        {
            if (!_activeObjects.Contains(goA) || !_activeObjects.Contains(goB) || AllowSiblingCollisionTrigger == SiblingCollision.All)
                return true;
            else if (AllowSiblingCollisionTrigger == SiblingCollision.None)
                return false;
            else if (AllowSiblingCollisionTrigger == SiblingCollision.Single & _collidedThisUpdate.Add(goA) | _collidedThisUpdate.Add(goB))
                return true;
            else
                return false;
        }

        public bool ContactAllowed(GameObject goA, GameObject goB)
        {
            return !_activeObjects.Contains(goA) || AllowSiblingSurfaceContact || !_activeObjects.Contains(goB);
        }

        #endregion
    }
}