using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

using UnityEngine;

using System.IO;

using Unity.Entities;

using NaughtyAttributes;

using PlaneWaver.Emitters;

namespace PlaneWaver.Library
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioLibrary : MonoBehaviour
    {
        #region FIELDS & PROPERTIES
        
        private bool _initialised;
        public static AudioLibrary Instance;
        public List<AudioObject> AudioObjects;
        public AudioSource PreviewAudioSource => GetComponent<AudioSource>();

        [Dropdown("CreateAudioObjectDropdown")] public AudioObject PreviewAudioObject;

        public int LibrarySize => AudioObjects.Count;

        #endregion

        #region LIBRARY INITIALISATION

        private void Awake()
        {
            Instance = this;
            PreviewAudioSource.Stop();
            PreviewAudioSource.clip = null;
            PreviewAudioSource.playOnAwake = false;
        }

        private void Start()
        {
            if (InitialiseLibrary())
                BuildAudioBlobEntities();
        }

        private bool InitialiseLibrary()
        {
            if (_initialised)
                return true;

            for (int i = AudioObjects.Count - 1; i >= 0; i--)
            {
                if (AudioObjects[i].Clip == null)
                    AudioObjects.RemoveAt(i);
            }

            if (AudioObjects.Count == 0)
            {
                Debug.LogError("No AudioObjects found in the Audio Library.");
                _initialised = false;
                return false;
            }

            _initialised = true;
            return true;
        }

        #endregion

        #region AUDIO ENTITIES

        private void BuildAudioBlobEntities()
        {
            if (!_initialised)
                return;

            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            foreach (AudioObject asset in AudioObjects)
                AudioBlob.BuildBlob(manager, asset);
        }

        #endregion

        #region ASSET MANAGEMENT

        public DropdownList<AudioObject> CreateAudioObjectDropdown()
        {
            DropdownList<AudioObject> list = new();
            foreach (AudioObject asset in AudioObjects) list.Add(asset.name, asset);
            return list;
        }

        public void LoadAssets()
        {
            AudioObjects = new();
            string[] assetGUIDs = AssetDatabase.FindAssets("t: ", new[] { LibraryConfig.AudioFilePath });

            IEnumerable<AudioObject> loadedAssets =
                    Resources.LoadAll(LibraryConfig.AudioObjectPath, typeof(AudioObject)).Cast<AudioObject>();
            AudioObjects.AddRange(loadedAssets);
        }

        public void BuildAudioObjects()
        {
            if (!AssetDatabase.IsValidFolder(LibraryConfig.AudioObjectPath))
                Directory.CreateDirectory(LibraryConfig.AudioObjectPath);

            string[] assetGUIDs = AssetDatabase.FindAssets(
                LibraryConfig.SourceTypeFilter,
                new[] { LibraryConfig.AudioFilePath }
            );
            AudioObjects = new();

            foreach (string t in assetGUIDs)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(t);

                string clipName = AssetParsing.ParseAssetPath(
                    filePath,
                    out PlaybackType clipType,
                    out string clipTypeFolder
                );
                var newAudioAsset = (AudioObject)ScriptableObject.CreateInstance(typeof(AudioObject));
                var clip = (AudioClip)AssetDatabase.LoadAssetAtPath(filePath, typeof(AudioClip));
                string[] tags = AssetParsing.ParseAssetTags(clipName);
                newAudioAsset.AssignAudioClip(clipName, tags, clip, clipType, AudioObjects.Count);
                
                AudioObjects.Add(newAudioAsset);

                AssetDatabase.CreateAsset(
                    newAudioAsset,
                    clipTypeFolder +
                    "/Audio." +
                    Enum.GetName(typeof(PlaybackType), clipType) +
                    "." +
                    clipName +
                    ".asset"
                );
                EditorUtility.SetDirty(newAudioAsset);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Audio Library has been rebuilt with '{AudioObjects.Count}' Audio Assets.");
        }
        
        public void CreateEmitterAsset(AudioObject audioObject, bool isVolatile)
        {
            if (audioObject == null)
                throw new NullReferenceException("AudioObject cannot be null.");
            if (!audioObject.ValidClip)
                throw new NullReferenceException("AudioObject clip asset is null.");

            string emitterType = isVolatile ? "Volatile" : "Stable";
            string assetPath = LibraryConfig.EmitterObjectPath + $"/{emitterType}";
            if (!AssetDatabase.IsValidFolder(assetPath))
                Directory.CreateDirectory(assetPath);

            EmitterObject newEmitter;
            
            if (isVolatile)
            {
                newEmitter = (VolatileEmitterObject)ScriptableObject.CreateInstance(typeof(VolatileEmitterObject));
                newEmitter.AudioObject = audioObject;
            }
            else
            {
                newEmitter = (StableEmitterObject)ScriptableObject.CreateInstance(typeof(StableEmitterObject));
                newEmitter.AudioObject = audioObject;
            }

            string shortName = audioObject.Tags.Length > 1
                    ? audioObject.Tags[0] + "." + audioObject.Tags[1]
                    : audioObject.Tags[0];
            
            AssetDatabase.CreateAsset(
                newEmitter,
                assetPath +
                $"/Emitter.{emitterType}." +
                shortName +
                ".asset"
            );
            EditorUtility.SetDirty(newEmitter);
            AssetDatabase.Refresh();
            Debug.Log($"Created new {emitterType} emitter: '{newEmitter.name}'");
            EditorGUIUtility.PingObject(newEmitter);
        }

        #endregion
    }
}