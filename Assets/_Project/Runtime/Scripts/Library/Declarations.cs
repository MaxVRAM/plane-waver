using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using UnityEditor;
using Unity.Entities;
using Unity.Collections;

using PlaneWaver.DSP;

namespace PlaneWaver.Library
{
    #region TYPE AND STATIC DECLARATIONS
    
    [Serializable]
    public enum PlaybackType
    {
        Default = 0,
        OneShot = 1,
        Short = 2,
        Long = 3,
        Loop = 4
    }

    public static class LibraryConstants
    {
        public const string ClipPrefix = "_AudioClip";
        public const string AudioFilePath = "Assets/_Project/Audio/Wav/";
        public const string AudioAssetPath = "Assets/_Project/Audio/Assets/";
        public const string AssetFilter = "t:AudioClip";
        public const string Extension = ".wav";
    }

    #endregion
    
    #region ASSET PARSING

    public struct AssetParsing
    {
        public static PlaybackType ParseTypeAndPrepareDirectory(string clipTypeString, ref string clipTypeFolder)
        {
            if (!Enum.TryParse(clipTypeString, out PlaybackType playbackType))
                playbackType = PlaybackType.Default;

            clipTypeFolder += playbackType + "/";

            if (!AssetDatabase.IsValidFolder(clipTypeFolder))
                Directory.CreateDirectory(clipTypeFolder);

            return playbackType;
        }
    
        public static string ParseAssetPath(string filePath, out PlaybackType clipType, out string clipTypeFolder)
        {
            string fileName = filePath;
            fileName = fileName.Remove(0, filePath.LastIndexOf('/') + 1);
            string clipTypeString = fileName[..fileName.IndexOf("_", StringComparison.Ordinal)];
            clipTypeString = Regex.Replace(clipTypeString, @"[^\w]*", string.Empty);
            string clipName = fileName[(fileName.IndexOf("_", StringComparison.Ordinal) + 1)..];
            clipName = clipName.Remove(clipName.IndexOf(LibraryConstants.Extension, StringComparison.Ordinal)).ToLower();
            clipTypeFolder = LibraryConstants.AudioAssetPath;
            clipType = ParseTypeAndPrepareDirectory(clipTypeString, ref clipTypeFolder);
            return clipName;
        }
    }

    #endregion
    
    #region BLOB THE AUDIO BUILDER
    
    public static class AudioBlob
    {
        public static bool BuildBlob(EntityManager manager, AudioAsset asset)
        {
            if (!asset.ValidClip)
                return false;

            Entity audioBlobEntity = manager.CreateEntity();
            
            var clipData = new float[asset.SampleCount];
            asset.Clip.GetData(clipData, 0);

            using var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref FloatBlobAsset audioClipBlobAsset = ref blobBuilder.ConstructRoot<FloatBlobAsset>();

            BlobBuilderArray<float> sampleArray = blobBuilder.Allocate(ref audioClipBlobAsset.Array, (asset.SampleCount / asset.Channels));
            GetAudioClipSampleBlob(ref sampleArray, clipData, asset.Channels);

            BlobAssetReference<FloatBlobAsset> audioClipBlobAssetRef = blobBuilder.CreateBlobAssetReference<FloatBlobAsset>(Allocator.Persistent);
            manager.AddComponentData(audioBlobEntity, new AssetSampleArray 
            { 
                AssetIndex = asset.ClipEntityIndex,
                SampleBlob = audioClipBlobAssetRef
            });

#if UNITY_EDITOR
            manager.SetName(audioBlobEntity, LibraryConstants.ClipPrefix + "." + asset.ClipEntityIndex + "." + asset.Clip.name);
#endif
            return true;
        }
        
        private static void GetAudioClipSampleBlob(ref BlobBuilderArray<float> audioBlob, IReadOnlyList<float> samples, int channels)
        {
            for (var s = 0; s < samples.Count - 1; s += channels)
            {
                audioBlob[s / channels] = 0;
                for (var c = 0; c < channels; c++)
                    audioBlob[s / channels] += samples[s + c] / channels;
            }
        }
    }
    
    #endregion
}