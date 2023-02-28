using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
using Unity.Entities;

namespace PlaneWaver.Library
{
    [RequireComponent(typeof(AudioSource))]
    public partial class AssetLibrary : MonoBehaviour
    {
        #region FIELDS & PROPERTIES

        public static AssetLibrary Instance;
        public AudioSource PreviewAudioSource;
        private bool _initialised;
        
        [NonSerialized] public List<AudioAsset> AudioAssets = new();
        
        public int LibrarySize => AudioAssets.Count;

        #endregion

        #region LIBRARY INITIALISATION

        private void Awake()
        {
            Instance = this;
            if (InitialiseLibrary())
                BuildAudioEntities();
        }

        public bool InitialiseLibrary()
        {
            if (_initialised)
                return true;

            for (int i = AudioAssets.Count - 1; i >= 0; i--)
            {
                if (AudioAssets[i].Clip == null)
                    AudioAssets.RemoveAt(i);
            }

            if (AudioAssets.Count == 0)
            {
                Debug.LogError("No AudioAsset objects found in the Audio Library.");
                _initialised = false;
                return false;
            }

            _initialised = true;
            return true;
        }

        #endregion

        #region AUDIO ENTITIES
        
        public void BuildAudioEntities()
        {
            if (!_initialised)
                return;

            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            foreach (AudioAsset asset in AudioAssets)
                AudioBlob.BuildBlob(manager, asset);
        }

        #endregion

        #region ASSET MANAGEMENT

        public void ReloadAudioAssets()
        {
            AudioAssets = new();
            IEnumerable<AudioAsset> loadedAssets = Resources.LoadAll(LibraryConstants.AudioAssetPath, typeof(AudioAsset)).Cast<AudioAsset>();
            AudioAssets.AddRange(loadedAssets);
        }

        public void RebuildAudioAssets()
        {
            if (!AssetDatabase.IsValidFolder(LibraryConstants.AudioAssetPath))
                Directory.CreateDirectory(LibraryConstants.AudioAssetPath);

            string[] assetGUIDs = AssetDatabase.FindAssets(LibraryConstants.AssetFilter, new[] { LibraryConstants.AudioFilePath });
            AudioAssets = new();

            for (var i = 0; i < assetGUIDs.Length; i++)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
                string clipName = AssetParsing.ParseAssetPath(filePath, out PlaybackType clipType, out string clipTypeFolder);
                
                var newAudioAsset = (AudioAsset)ScriptableObject.CreateInstance(typeof(AudioAsset));
                var clip = (AudioClip)AssetDatabase.LoadAssetAtPath(filePath, typeof(AudioClip));
                
                AudioAssets.Add(newAudioAsset.AssignAudioClip(clip, clipType, AudioAssets.Count));
                AssetDatabase.CreateAsset(newAudioAsset, clipTypeFolder + "Audio_" +
                    Enum.GetName(typeof(PlaybackType), clipType) + "_" + clipName + ".asset");
                
                EditorUtility.SetDirty(newAudioAsset);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Audio Library has been rebuilt with '{AudioAssets.Count()}' Audio Assets.");
        }
        
        #endregion
    }
}
