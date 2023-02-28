using System;

using Unity.Entities;

using UnityEditor;
using UnityEngine;

namespace PlaneWaver
{
    /// <summary>
    /// Scriptable Object for creating audio clip assets with assignable types and other properties.
    /// Will be used for expanding audio library paradigm to make it easier to manage and
    /// assign audio assets to interactive synthesis elements.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioClipAsset", menuName = "Plane Waver/Asset/Audio Clip", order = 1)]
    public class AudioAssetScriptable : ScriptableObject
    {
        #region FIELDS & PROPERTIES

        public AudioClip _Clip;
        public AudioClipType ClipType;
        public int ClipEntityIndex;
        public int SampleRate;
        public int SampleCount;
        public float Duration;
        
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
            ClipEntityIndex = entityIndex;
        }

        public AudioAssetScriptable AssociateAudioClip(AudioClip clip, AudioClipType clipType, int index)
        {
            _Clip = clip;
            ClipType = clipType;
            ClipEntityIndex = index;

            return !UpdateClipProperties() ? null : this;
        }

        private bool UpdateClipProperties()
        {
            if (_Clip == null)
                return false;

            SampleRate = _Clip.frequency;
            SampleCount = _Clip.samples;
            Duration = _Clip.length;
            return true;
        }

        #endregion
    }
}
