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
        [SerializeField] private float _SecondsSinceSpawn = 0;
        [SerializeField] public HashSet<GameObject> _CollidedThisUpdate;

        [Header("Object Configuration")]
        [Tooltip("Object providing the spawn location and controller behaviour.")]
        public GameObject _ControllerObject;
        [Tooltip("Parent GameObject to attach spawned prefabs.")]
        public GameObject _SpawnableHost;
        [Tooltip("Prefab to spawn.")]
        [SerializeField] private GameObject _PrefabToSpawn;
        [SerializeField] private bool _RandomiseSpawnPrefab = false;
        [Tooltip("A list of prebs to spawn can also be supplied, allowing runtime selection of object spawn selection.")]
        [SerializeField] private List<GameObject> _SpawnablePrefabs;
        public List<GameObject> _ActiveObjects = new();

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
        [Tooltip("Emissive brightness range to modulate associated renderers. X = base emissive brightness, Y = brightness on event trigger.")]
        [EnableIf("VisualFeedbackOn")]
        [MinMaxSlider(-10f, 10f)] public Vector2 _EmissiveBrightness = new(0, 10);
        [EnableIf("VisualFeedbackOn")]
        [Range(0, 1)][SerializeField] private float _EmissiveFlashFade = 0.5f;
        [Tooltip("Supply list of renderers to modulate/flash emissive brightness on selected triggers.")]
        [SerializeField] private List<Renderer> _ControllerRenderers = new();
        private List<Color> _EmissiveColours = new();
        private float _EmissiveIntensity = 0;
        public bool VisualFeedbackOn => _EmissiveFlashTrigger is not ControllerEvent.Off;

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

            foreach (Renderer renderer in _ControllerRenderers)
            {
                Color colour = renderer.material.GetColor("_EmissiveColor");
                _EmissiveColours.Add(colour);
            }

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

            _Initialised = true;
            _StartTime = Time.time + _SpawnDelaySeconds;
            return true;
        }

        #endregion

        #region UPDATE SCHEDULE

        void Update()
        {
            _SpawnTimer.UpdateTrigger(Time.deltaTime, _SpawnPeriodSeconds);

            if (ReadyToSpawn())
            {
                if (_AutoSpawn) CreateSpawnable();
                else if (_AutoRemove) RemoveSpawnable(0);
            }

            _ActiveObjects.RemoveAll(item => item == null);
            UpdateShaderModulation();
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
            switch (_SpawnCondition)
            {
                case SpawnCondition.Never:
                    return false;
                case SpawnCondition.Always:
                    return true;
                case SpawnCondition.AfterSpeakersPopulated:
                    return !GrainBrain.Instance.PopulatingSpeakers;
                case SpawnCondition.IfSpeakerAvailable:
                    return !GrainBrain.Instance._Speakers.TrueForAll(s => s.IsActive);
                case SpawnCondition.AfterDelayPeriod:
                    return _StartTimeReached || (_StartTimeReached = Time.time > _StartTime);
                default:
                    return false;
            }
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
                _EmissiveIntensity = _EmissiveBrightness.y;
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
            Quaternion directionRotation = Quaternion.FromToRotation(Vector3.up, spawnDirection);

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
        }

        #endregion

        #region RUNTIME MODULATIONS

        public void UpdateShaderModulation()
        {
            for (int i = 0; i < _ControllerRenderers.Count; i++)
            {
                _ControllerRenderers[i].material.SetColor("_EmissiveColor", _EmissiveColours[i] * _EmissiveIntensity * 2);
            }

            float glow = _EmissiveBrightness.x + (1 + Mathf.Sin(_SecondsSinceSpawn / _SpawnPeriodSeconds * 2)) * 0.5f;
            _EmissiveIntensity = _EmissiveIntensity.Smooth(glow, _EmissiveFlashFade);
        }

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

        #endregion
    }
}