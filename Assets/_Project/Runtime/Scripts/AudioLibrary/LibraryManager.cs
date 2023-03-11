using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using NaughtyAttributes;

namespace PlaneWaver.Library
{
    [RequireComponent(typeof(AudioSource))]
    public class LibraryManager : MonoBehaviour
    {
        #region FIELDS & PROPERTIES

        private bool _initialised;
        public static LibraryManager Instance;
        public List<AudioObject> AudioObjects;
        public AudioSource AudioSource;

        [Dropdown("CreateAudioObjectDropdown")]
        public AudioObject PreviewAudioObject;

        public int LibrarySize => AudioObjects.Count;

        #endregion

        #region LIBRARY INITIALISATION

        private void Awake()
        {
            Instance = this;
            if (!TryGetComponent(out AudioSource))
                AudioSource = gameObject.AddComponent<AudioSource>();
            AudioSource.Stop();
            AudioSource.clip = null;
            AudioSource.playOnAwake = false;
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

        #region ASSET MANAGEMENT

        private void BuildAudioBlobEntities()
        {
            if (!_initialised)
                return;

            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            foreach (AudioObject asset in AudioObjects)
                LibraryUtilities.AudioBlob.BuildBlob(manager, asset);
        }

        public DropdownList<AudioObject> CreateAudioObjectDropdown()
        {
            if (AudioObjects == null || AudioObjects.Count == 0)
                return new DropdownList<AudioObject>{{"No AudioObjects found", null}};
            
            AudioObjects.RemoveAll(item => item == null);

            DropdownList<AudioObject> list = new();
            foreach (AudioObject asset in AudioObjects)
                list.Add(asset.name, asset);
            return list;
        }
        
        #endregion
    }
}