using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEngine;

using GD.MinMaxSlider;

using MaxVRAM;
using MaxVRAM.Counters;

using PlaneWaver.Emitters;
using Unity.Entities;

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

        [Header("Object Configuration")]
        [Tooltip("Transform with an attached joint component to anchor the controller object.")]
        public Transform ControllerAnchor;
        [Tooltip("Transform of the game object to act as the spawner.")]
        public Transform ControllerTransform;
        [Tooltip("Parent transform to attach spawned prefabs.")]
        public Transform SpawnParentTransform;
        [Tooltip("Prefab to spawn.")]
        public GameObject PrefabToSpawn;
        public BaseJointObject AttachmentJoint;

        private const int MaxObjects = 100;
        [Header("Spawning Rules")] [Range(0, 100)]
        public int TargetNumObjects = 10;
        [Tooltip("Delay (seconds) between each spawn.")] [Range(0.01f, 1)]
        public float SpawnPeriodSeconds = 0.2f;
        public SpawnCondition SpawnWhen = SpawnCondition.AfterSpeakersPopulated;
        public bool SpawnAfterPeriodSet => SpawnWhen == SpawnCondition.AfterDelayPeriod;
        [Tooltip("Number of seconds after this ObjectSpawner is created before it starts spawning loop.")]
        public float SpawnDelaySeconds = 2;
        public bool AutoSpawn = true;
        public bool AutoRemove = true;

        private float _startTime = int.MaxValue;
        private bool _startTimeReached;
        private CountTrigger _spawnTimer;

        [Header("Object Properties")]
        [SerializeField]
        [MinMaxSlider(0.01f, 10f)]
        public Vector2 SpawnObjectScale = new(1, 1);

        [Header("Spawn Position")]
        [Tooltip("Spawn position relative to the controller object. Converts to unit vector.")]
        public Vector3 EjectionPosition = new(1, 0, 0);
        [Tooltip("Apply randomisation to the spawn position unit vector.")] [Range(0f, 1f)]
        public float PositionVariance;
        [Tooltip("Distance from the controller to spawn objects.")]
        [MinMaxSlider(0,10)]
        public Vector2 EjectionRadius = new(1, 2);

        [Header("Spawn Velocity")]
        public Vector3 EjectionDirection = new(0, 0, 1);
        [Tooltip("Apply randomisation to the spawn direction.")] [Range(0f, 1f)]
        public float DirectionVariance;
        [Tooltip("Speed that spawned objects leave the controller.")]
        [MinMaxSlider(0,40)]
        public Vector2 EjectionSpeed = new(0, 2);

        [Header("Object Removal")]
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
        
        public List<GameObject> SpawnedObjectPool;
        public int PooledObjectCount;
        private int ActiveObjectCount => MaxObjects - PooledObjectCount;

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
            _spawnTimer = new CountTrigger(TimeUnit.Seconds, SpawnPeriodSeconds);
            SpawnedObjectPool = new List<GameObject>();
        }
        
        private void OnDisable()
        {
            SpawnedObjectPool?.ForEach(o => o.SetActive(false));
            if (SpawnedObjectPool != null)
                PooledObjectCount = SpawnedObjectPool.Count;
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

            if (PrefabToSpawn == null)
            {
                Debug.LogWarning($"{name} has not been assigned a prefab to spawn.");
                return _initialised = false;
            }
            
            SpawnedObjectPool = new List<GameObject>();
            
            for (var i = 0; i < MaxObjects; i++)
            {
                GameObject tempObject = Instantiate(PrefabToSpawn, SpawnParentTransform);
                tempObject.SetActive(false);
                tempObject.name = string.Format("{0}.{1}", name, "spawned");
                SpawnedObjectPool.Add(tempObject);
                PooledObjectCount++;
            }

            _initialised = true;
            _startTime = Time.time + SpawnDelaySeconds;
            return true;
        }

        #endregion

        #region UPDATE METHODS

        private void Update()
        {
            _spawnTimer.UpdateTrigger(Time.deltaTime, SpawnPeriodSeconds);
            
            if (!ReadyToSpawn()) return;
            if (AutoSpawn) SpawnPooledObject();
            if (AutoRemove) DisableActiveObject(0);
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

            if (GetPooledObjectIndex(out int index) == -1)
                return;
            
            InitialiseObjectTransform(index);
            InitialiseSpawnVelocity(index);
            ConfigureObjectComponents(index, ControllerTransform);
            SpawnedObjectPool[index].SetActive(true);
            PooledObjectCount--;

            if (EmissiveFlashTrigger == ControllerEvent.OnSpawn)
                MaterialModulator.Flash();
        }

        private void DisableActiveObject(int index)
        {
            if (ActiveObjectCount <= TargetNumObjects ||
                !_spawnTimer.DrainTrigger())
                return;

            if (index < MaxObjects && SpawnedObjectPool[index].activeInHierarchy)
                SpawnedObjectPool[index].SetActive(false);
        }

        public void ObjectPooled()
        {
            PooledObjectCount++;
        }

        #endregion

        #region SPAWN CONFIGURATION

        private int GetPooledObjectIndex(out int index)
        {
            foreach (GameObject obj in SpawnedObjectPool)
            {
                if (obj.activeInHierarchy) continue;
                return index = SpawnedObjectPool.IndexOf(obj);
            }

            return index = -1;
        }

        private void InitialiseObjectTransform(int index)
        {
            float spawnDistance = Rando.Range(EjectionRadius);
            Vector3 randomPosition = Random.onUnitSphere;
            Vector3 unitPosition = Vector3.Slerp(EjectionPosition.normalized, randomPosition, PositionVariance);
            Vector3 positionLocal = unitPosition * spawnDistance;
            SpawnedObjectPool[index].transform.parent = SpawnParentTransform;
            SpawnedObjectPool[index].transform.position = ControllerTransform.position + positionLocal;
            SpawnedObjectPool[index].transform.rotation = Quaternion.LookRotation(unitPosition);
            SpawnedObjectPool[index].transform.localScale = PrefabToSpawn.transform.localScale * Rando.Range(SpawnObjectScale);
        }

        private void InitialiseSpawnVelocity(int index)
        {
            if (!SpawnedObjectPool[index].TryGetComponent(out Rigidbody rb))
                rb = SpawnedObjectPool[index].AddComponent<Rigidbody>();
            
            Vector3 randomDirection = Random.onUnitSphere;

            Vector3 spawnDirectionUnitVector = Vector3.Slerp(
                EjectionDirection.normalized,
                randomDirection,
                DirectionVariance
            );

            Vector3 velocity = SpawnedObjectPool[index].transform.localRotation * spawnDirectionUnitVector * 
                               (EjectionDirection.magnitude * Rando.Range(EjectionSpeed));
            rb.velocity = velocity;
            rb.angularVelocity = Vector3.zero;
            
            if (PrefabToSpawn.TryGetComponent(out Rigidbody prefabRb))
                rb.mass = prefabRb.mass * SpawnedObjectPool[index].transform.localScale.magnitude;
        }

        private void ConfigureObjectComponents(int index, Transform controllerTransform)
        {
            ActorObject actor = SpawnedObjectPool[index].GetComponent<ActorObject>() ??
                                SpawnedObjectPool[index].AddComponent<ActorObject>();
            
            actor.Spawner = this;
            actor.OtherBody = controllerTransform;
            
            actor.ControllerData = new ActorControllerData(
                UseSpawnLifespan ? SpawnLifespan : -1,
                BoundingRadius,
                BoundingAreaType,
                BoundingCollider,
                controllerTransform
            );

            EmitterFrame[] attachedEmitterFrames = SpawnedObjectPool[index].GetComponentsInChildren<EmitterFrame>();
            
            if (attachedEmitterFrames.Length > 1)
                Debug.LogWarning($"ActorSpawner {name} has more than one EmitterFrame attached to it. " +
                                 "This is not supported and may cause unexpected behaviour. " +
                                 "Please ensure that only one EmitterFrame is attached to the object.");

            foreach (EmitterFrame frame in attachedEmitterFrames)
            {
                frame.Actor = actor;
                frame.EntityIndex = index;
            }
            
            var jointController = SpawnedObjectPool[index].GetComponent<JointController>();

            if (AttachmentJoint == null)
            {
                if (jointController != null)
                    Destroy(jointController);
                return;
            }
            
            if (jointController == null)
                jointController = SpawnedObjectPool[index].AddComponent<JointController>();
            
            jointController.Initialise(AttachmentJoint, controllerTransform);
        }

        #endregion

        #region COLLISION MANAGEMENT

        public bool CollisionAllowed(GameObject objA, GameObject objB)
        {
            if (!SpawnedObjectPool.Contains(objA) ||
                !SpawnedObjectPool.Contains(objB) ||
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
            return !SpawnedObjectPool.Contains(goA) || AllowSiblingSurfaceContact || !SpawnedObjectPool.Contains(goB);
        }

        #endregion
    }
}