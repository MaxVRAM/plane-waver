using System;
using UnityEditor;
using UnityEngine;

namespace PlaneWaver
{
    /// <summary>
    /// Scriptable Object for creating audio clip assets with assignable types and other properties.
    /// Will be used for expanding audio library paradigm to make it easier to manage and
    /// assign audio assets to interactive synthesis elements.
    /// </summary>
    [CreateAssetMenu(fileName = "Audio", menuName = "Plane Waver/Audio Asset")]
    public class AudioAssetScriptable : ScriptableObject
    {
        #region FIELDS & PROPERTIES

        [SerializeField] private AudioClip _Clip;
        [SerializeField] private AudioClipType _ClipType;
        [SerializeField] private int _ClipEntityIndex;
        [SerializeField] DateTime _DateCreated;
        private GUID _GUID;
        public int _SampleRate;
        public int _SampleCount;
        public float _Duration;

        public int ClipEntityIndex => _ClipEntityIndex;
        public AudioClipType ClipType => _ClipType;
        public AudioClip Clip => _Clip;
        public string ClipName => _Clip.name;
        public bool ValidClip => _Clip != null;
        public int SampleRate => _SampleRate;
        public int Samples => _SampleCount;
        public float Duration => _Duration;

        #endregion

        #region INITIALISATION

        private void Awake()
        {
            UpdateClipProperties();
        }

        void OnValidate()
        {
            UpdateClipProperties();
        }

        public void UpdateEntityIndex(int entityIndex)
        {
            _ClipEntityIndex = entityIndex;
        }

        public AudioAssetScriptable AssociateAudioClip(AudioClip clip, AudioClipType clipType, int index)
        {
            _Clip = clip;
            _ClipType = clipType;
            _ClipEntityIndex = index;

            if (!UpdateClipProperties())
                return null;

            _DateCreated = DateTime.Now;
            return this;
        }

        private bool UpdateClipProperties()
        {
            if (_Clip == null)
                return false;

            _SampleRate = _Clip.frequency;
            _SampleCount = _Clip.samples;
            _Duration = _Clip.length;
            return true;
        }

        #endregion
    }
}
