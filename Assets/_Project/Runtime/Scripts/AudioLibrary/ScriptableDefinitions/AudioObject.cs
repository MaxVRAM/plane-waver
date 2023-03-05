
using System.Collections.Generic;

using UnityEngine;

namespace PlaneWaver.Library
{
    /// <summary>
    /// Scriptable Object for creating audio clip assets with assignable types and other properties.
    /// Will be used for expanding audio library paradigm to make it easier to manage and
    /// assign audio assets to interactive synthesis elements.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioObject.", menuName = "PlaneWaver/Audio Library/Audio Object", order = 1)]
    public class AudioObject : ScriptableObject
    {
        #region FIELDS & PROPERTIES

        public string Name;
        public string[] Tags;
        public AudioClip Clip;
        public PlaybackType Playback;
        public int ClipEntityIndex;

        public int SampleRate => Clip.frequency;
        public int SampleCount => Clip.samples;
        public float Duration => Clip.length;
        public int Channels => Clip.channels;
        public bool ValidClip => Clip != null;
        
        #endregion

        #region INITIALISATION
        
        public void UpdateEntityIndex(int entityIndex)
        {
            ClipEntityIndex = entityIndex;
        }

        public AudioObject AssignAudioClip(string assetName, string[] tags,
                                           AudioClip clip, PlaybackType playback, int index)
        {
            Name = assetName;
            Tags = tags;
            Clip = clip;
            Playback = playback;
            ClipEntityIndex = index;
            return ValidClip ? this : null;
        }

        #endregion
    }
}
