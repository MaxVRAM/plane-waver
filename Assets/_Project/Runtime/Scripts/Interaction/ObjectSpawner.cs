using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GD.MinMaxSlider;

using MaxVRAM;
using MaxVRAM.Counters;
using PlaneWaver.Emitters;
using Random = UnityEngine.Random;

namespace PlaneWaver.Interaction
{
    /// <summary>
    /// Manager for spawning Actor-based game objects with dynamically assigned emitter and interactive configurations.
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        #region MEMBERS & PROPERTIES

        [Header("Runtime Dynamics")] private bool _initialised;
        private HashSet<GameObject> _collidedThisUpdate;

        [Header("Object Configuration")] [Tooltip("Object providing the spawn location and controller behaviour.")]
        public Transform ControllerTransform;
        public Transform ControllerAnchor;
        [Tooltip("Parent transform to attach spawned prefabs.")]
        public Transform SpawnParentTransform;
        [Tooltip("Prefab to spawn.")] public GameObject PrefabToSpawn;
        [Tooltip(
            "A list of prefabs to spawn can also be supplied, allowing runtime selection of object spawn selection."
        )]
        public List<GameObject> SpawnablePrefabs;
        public bool RandomiseSpawnPrefab;
        public BaseJointScriptable AttachmentJoint;

        private const int MaxObjects = 100;
        [Header("Spawning Rules")] [Range(0, 100)]
        public int TargetNumObjects = 10;
        [Tooltip("Delay (seconds) between each spawn.")] [Range(0.01f, 1)]
        public float SpawnPeriodSeconds = 0.2f;
        public SpawnCondition SpawnWhen = SpawnCondition.AfterSpeakersPopulated;
        public bool SpawnAfterPeriodSet => SpawnWhen == SpawnCondition.AfterDelayPeriod;
        //[EnableIf("SpawnAfterPeriodSet")]
        [Tooltip("Number of seconds after this ObjectSpawner is created before it starts spawning loop.")]
        public float SpawnDelaySeconds = 2;
        public bool AutoSpawn = true;
        public bool AutoRemove = true;

        private float _startTime = int.MaxValue;
        private bool _startTimeReached;
        private CountTrigger _spawnTimer;

        [Header("Object Properties")] [SerializeField] //[MinMaxSlider(0.01f, 2)]
        [MinMaxSlider(0.01f, 2f)] public Vector2 SpawnObjectScale = new(1, 1);

        [Header("Spawn Position")]
        [Tooltip("Spawn position relative to the controller object. Converts to unit vector.")]
        public Vector3 EjectionPosition = new(1, 0, 0);
        [Tooltip("Apply randomisation to the spawn position unit vector.")] [Range(0f, 1f)]
        public float PositionVariance;
        [Tooltip("Distance from the controller to spawn objects.")] //[MinMaxSlider(0f, 10f)]
        [MinMaxSlider(0,10)]public Vector2 EjectionRadius = new(1, 2);

        [Header("Spawn Velocity")] //[MinValue(-1)] [MaxValue(1)]
        public Vector3 EjectionDirection = new(0, 0, 1);
        [Tooltip("Apply randomisation to the spawn direction.")] [Range(0f, 1f)]
        public float DirectionVariance;
        [Tooltip("Speed that spawned objects leave the controller.")] //[MinMaxSlider(0f, 20)]
        [MinMaxSlider(0,20)] public Vector2 EjectionSpeed = new(0, 2);

        [Header("Object Removal")]
        [Tooltip(
            "Coordinates that define the bounding for spawned objects, which are destroyed if they leave. The bounding radius is ignored when using Collider Bounds, defined instead by the supplied collider bounding area, deaulting to the controller's collider if it has one."
        )]
        public ActorBounds BoundingAreaType = ActorBounds.ControllerTransform;
        public Collider BoundingCollider;
        [Tooltip("Radius of the bounding volume.")]
        public float BoundingRadius = 30f;
        [Tooltip("Use a timer to destroy spawned objects after a duration.")]
        public bool UseSpawnLifespan = true;
        [Tooltip("Duration in seconds before destroying spawned object.")]
        [MinMaxSlider(0,60)] public Vector2 SpawnLifeSpanDuration = new(5, 10);
        private float SpawnLifespan => UseSpawnLifespan ? Rando.Range(SpawnLifeSpanDuration) : -1;
        public bool UsingBoundingRadius =>
                BoundingAreaType is ActorBounds.SpawnPosition or ActorBounds.ControllerTransform;
        public bool UsingColliderBounds => BoundingAreaType is ActorBounds.ColliderBounds;

        [Header("Emitter Behaviour")] public bool AllowSiblingSurfaceContact = true;
        public SiblingCollision AllowSiblingCollisionTrigger = SiblingCollision.Single;

        [Header("Visual Feedback")] public ControllerEvent EmissiveFlashTrigger = ControllerEvent.OnSpawn;
        public MaterialColourModulator MaterialModulator;
        
        public List<GameObject> _spawnedObjectPool;
        public int PooledObjectCount;
        public int ActiveObjectCount => MaxObjects - PooledObjectCount;
        //private List<GameObject> _activeObjects;

        #endregion

        #region INITIALISATION METHODS

        private void Start()
        {
            if (!InitialiseSpawner())
                gameObject.SetActive(false);
        }

        private void Awake()
        {
            // StartCoroutine(ClearCollisions());
            // _spawnTimer = new CountTrigger(TimeUnit.Seconds, SpawnPeriodSeconds);
            // _activeObjects = new List<GameObject>();
        }
        
        private void OnEnable()
        {
            StartCoroutine(ClearCollisions());
            _spawnTimer = new CountTrigger(TimeUnit.Seconds, SpawnPeriodSeconds);
            _startTime = Time.time;
            
            _spawnedObjectPool = new List<GameObject>();

            for (var i = 0; i < MaxObjects; i++)
            {
                GameObject tempObject = Instantiate(PrefabToSpawn, SpawnParentTransform);
                tempObject.SetActive(false);
                _spawnedObjectPool.Add(tempObject);
                PooledObjectCount++;
            }
        }

        private void OnDisable()
        {
            _spawnedObjectPool?.ForEach(Destroy);
        }

        private IEnumerator ClearCollisions()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                _collidedThisUpdate.Clear();
            }
            // ReSharper disable once IteratorNeverReturns
        }
        
        private bool InitialiseSpawner()
        {
            _collidedThisUpdate = new HashSet<GameObject>();

            if (ControllerTransform == null)
                ControllerTransform = transform;

            if (ControllerAnchor != null &&
                    ControllerTransform.TryGetComponent(out Rigidbody controllerRb) &&
                    ControllerAnchor.TryGetComponent(out SpringJoint anchorJoint))
                anchorJoint.connectedBody = controllerRb;

            if (SpawnParentTransform == null)
                SpawnParentTransform = transform;

            if (SpawnablePrefabs.Count == 0 &&
                PrefabToSpawn != null)
                SpawnablePrefabs.Add(PrefabToSpawn);

            if (SpawnablePrefabs.Count >= 1 && PrefabToSpawn == null)
                PrefabToSpawn = SpawnablePrefabs[0];

            if (SpawnablePrefabs.Count == 0)
            {
                Debug.LogWarning($"ActorSpawner cannot spawn without prefab defined. Assign a SpawnablePrefab to {name}.");
                return _initialised = false;
            }

            _initialised = true;
            _startTime = Time.time + SpawnDelaySeconds;
            return true;
        }

        #endregion

        #region UPDATE METHODS

        private void Update()
        {
            //RemoveLingeringObjects();

            _spawnTimer.UpdateTrigger(Time.deltaTime, SpawnPeriodSeconds);
            
            if (!ReadyToSpawn()) return;
            if (AutoSpawn) SpawnPooledObject();
            if (AutoRemove) DisableActiveObject(0);
        }

        #endregion

        #region SPAWNING MANAGEMENT

        // private void RemoveLingeringObjects()
        // {
        //     if (_activeObjects.Count == 0) return;
        //     _activeObjects.RemoveAll(item => item == null);
        // }
        
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
                SpawnCondition.AfterSpeakersPopulated => !SynthManager.Instance.PopulatingSpeakers,
                SpawnCondition.SpeakerAvailable => SynthManager.Instance.Speakers.FirstOrDefault(s => !s.IsActive),
                SpawnCondition.AfterDelayPeriod => _startTimeReached || (_startTimeReached = Time.time > _startTime),
                _ => false
            };
        }

        private void SpawnPooledObject()
        {
            if (ActiveObjectCount >= TargetNumObjects ||
                !_spawnTimer.DrainTrigger())
                return;

            if (!GetPooledObject(out GameObject obj))
                return;
            
            InitialiseObjectTransform(ref obj);
            ConfigureObjectComponents(obj, ControllerTransform);
            obj.SetActive(true);
            PooledObjectCount--;

            if (EmissiveFlashTrigger == ControllerEvent.OnSpawn)
                MaterialModulator.Flash();
        }

        private void DisableActiveObject(int index)
        {
            if (ActiveObjectCount <= TargetNumObjects ||
                !_spawnTimer.DrainTrigger())
                return;

            if (index < MaxObjects && _spawnedObjectPool[index].activeInHierarchy)
                _spawnedObjectPool[index].SetActive(false);
            
            // if (index < ActiveObjectCount)
            //     Destroy(_spawnedObjectPool[index]);
            // _spawnedObjectPool.RemoveAt(index);
        }

        public void ObjectPooled()
        {
            PooledObjectCount++;
        }

        #endregion

        #region SPAWN CONFIGURATION

        // private void InstantiatePrefab(out GameObject pooledObject)
        // {
        //     int index = !RandomiseSpawnPrefab || SpawnablePrefabs.Count < 2
        //             ? Random.Range(0, SpawnablePrefabs.Count)
        //             : 0;
        //     GameObject objectToSpawn = SpawnablePrefabs[index];
        //
        //     InitialiseObjectTransform(out Vector3 spawnPosition, out Quaternion spawnRotation);
        //     pooledObject = Instantiate(objectToSpawn, spawnPosition, spawnRotation, SpawnParentTransform);
        //     pooledObject.transform.localScale *= Rando.Range(SpawnObjectScale);
        //     ComputeSpawnVelocity(pooledObject);
        // }

        private bool GetPooledObject(out GameObject ojb)
        {
            foreach (GameObject obj in _spawnedObjectPool)
            {
                if (obj.activeInHierarchy) continue;
                ojb = obj;
                return true;
            }

            ojb = null;
            return false;
        }

        private void InitialiseObjectTransform(ref GameObject obj)
        {
            float spawnDistance = Rando.Range(EjectionRadius);
            Vector3 randomPosition = Random.onUnitSphere;
            Vector3 unitPosition = Vector3.Slerp(EjectionPosition.normalized, randomPosition, PositionVariance);
            Vector3 positionLocal = unitPosition * spawnDistance;
            obj.transform.parent = SpawnParentTransform;
            obj.transform.position = ControllerTransform.position + positionLocal;
            obj.transform.rotation = Quaternion.LookRotation(unitPosition);
            obj.transform.localScale *= Rando.Range(SpawnObjectScale);
            InitialiseSpawnVelocity(obj);
        }

        private void InitialiseSpawnVelocity(GameObject obj)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>() ?? obj.AddComponent<Rigidbody>();
            Vector3 randomDirection = Random.onUnitSphere;

            Vector3 spawnDirectionUnitVector = Vector3.Slerp(
                EjectionDirection.normalized,
                randomDirection,
                DirectionVariance
            );

            Vector3 velocity = obj.transform.localRotation * 
                               spawnDirectionUnitVector * 
                               (EjectionDirection.magnitude * Rando.Range(EjectionSpeed));
            rb.velocity = velocity;
        }

        private void ConfigureObjectComponents(GameObject obj, Transform controllerTransform)
        {
            if (AttachmentJoint != null)
                obj.AddComponent<InteractionJointController>().Initialise(AttachmentJoint, controllerTransform);

            ActorObject actor = obj.GetComponent<ActorObject>() ?? obj.AddComponent<ActorObject>();
            actor.Spawner = this;
            actor.OtherBody = controllerTransform;
            actor.ControllerData = new ActorControllerData(
                UseSpawnLifespan ? SpawnLifespan : -1,
                BoundingRadius,
                BoundingAreaType,
                BoundingCollider,
                controllerTransform
            );

            EmitterFrame[] spawnedObjectEmitterFrames = obj.GetComponentsInChildren<EmitterFrame>();
            if (spawnedObjectEmitterFrames.Length <= 0) return;

            foreach (EmitterFrame frame in spawnedObjectEmitterFrames)
                frame.Actor = actor;
        }

        #endregion

        #region COLLISION MANAGEMENT

        public bool CollisionAllowed(GameObject objA, GameObject objB)
        {
            if (!_spawnedObjectPool.Contains(objA) ||
                !_spawnedObjectPool.Contains(objB) ||
                AllowSiblingCollisionTrigger == SiblingCollision.All)
                return true;

            if (AllowSiblingCollisionTrigger == SiblingCollision.None)
                return false;

            if (((AllowSiblingCollisionTrigger == SiblingCollision.Single) & _collidedThisUpdate.Add(objA)) |
                _collidedThisUpdate.Add(objB))
                return true;
            return false;
        }

        public bool ContactAllowed(GameObject goA, GameObject goB)
        {
            return !_spawnedObjectPool.Contains(goA) || AllowSiblingSurfaceContact || !_spawnedObjectPool.Contains(goB);
        }

        #endregion
    }
}