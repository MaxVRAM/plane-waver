using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;

using PlaneWaver.Emitters;
using PlaneWaver.DSP;

namespace PlaneWaver.Library
{
    public struct LibraryUtilities
    {
        #region LIBRARY POPULATION
        
        public static List<AudioObject> LoadAssets()
        {
            var audioObjects = new List<AudioObject>();
            string[] assetGUIDs = AssetDatabase.FindAssets
            ("t: ", new[] {
                LibraryConfig.AudioFilePath
            });

            IEnumerable<AudioObject> loadedAssets = Resources.LoadAll
                    (LibraryConfig.AudioObjectPath, typeof(AudioObject)).Cast<AudioObject>();
            audioObjects.AddRange(loadedAssets);

            return audioObjects;
        }

        #endregion
        
        #region LIBRARY CREATION

        public static List<AudioObject> BuildAudioObjects()
        {
            if (!AssetDatabase.IsValidFolder(LibraryConfig.AudioObjectPath))
                Directory.CreateDirectory(LibraryConfig.AudioObjectPath);

            string[] assetGUIDs = AssetDatabase.FindAssets
            (LibraryConfig.SourceTypeFilter, new[] {
                LibraryConfig.AudioFilePath
            });
            var audioObjects = new List<AudioObject>();

            foreach (string t in assetGUIDs)
            {
                string filePath = AssetDatabase.GUIDToAssetPath(t);

                string clipName = AssetParsing
                       .ParseAssetPath(filePath, out PlaybackType clipType, out string clipTypeFolder);
                var newAudioAsset = (AudioObject)ScriptableObject
                       .CreateInstance(typeof(AudioObject));
                var clip = (AudioClip)AssetDatabase
                       .LoadAssetAtPath(filePath, typeof(AudioClip));
                string[] tags = AssetParsing
                       .ParseAssetTags(clipName);
                newAudioAsset
                       .AssignAudioClip(clipName, tags, clip, clipType, audioObjects.Count);

                audioObjects.Add(newAudioAsset);

                AssetDatabase.CreateAsset
                (newAudioAsset,
                    clipTypeFolder + "/Audio." + Enum.GetName(typeof(PlaybackType), clipType) + "." + clipName + ".asset");
                EditorUtility.SetDirty(newAudioAsset);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Audio Library has been rebuilt with '{audioObjects.Count}' Audio Assets.");
            return audioObjects;
        }
        
        public static void CreateEmitterAsset(AudioObject audioObject, bool isVolatile)
        {
            if (audioObject == null)
                throw new NullReferenceException("AudioObject cannot be null.");

            if (!audioObject.ValidClip)
                throw new NullReferenceException("AudioObject clip asset is null.");

            string emitterType = isVolatile ? "Volatile" : "Stable";
            string assetPath = LibraryConfig.EmitterObjectPath + $"/{emitterType}";

            if (!AssetDatabase.IsValidFolder(assetPath))
                Directory.CreateDirectory(assetPath);

            BaseEmitterObject newEmitter;

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

            AssetDatabase.CreateAsset(newEmitter, assetPath + $"/Emitter.{emitterType}." + shortName + ".asset");
            EditorUtility.SetDirty(newEmitter);
            AssetDatabase.Refresh();
            Debug.Log($"Created new {emitterType} emitter: '{newEmitter.name}'");
            EditorGUIUtility.PingObject(newEmitter);
        }
        
        #endregion

        #region ASSET PARSING

        public struct AssetParsing
        {
            public static PlaybackType ParseTypeAndPrepareDirectory(string clipTypeString, ref string clipTypeFolder)
            {
                if (!Enum.TryParse(clipTypeString, true, out PlaybackType playbackType))
                    playbackType = PlaybackType.Default;

                clipTypeFolder += "/" + playbackType;

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
                clipName = clipName.Remove(
                    clipName.IndexOf(LibraryConfig.SourceExtensionFilter, StringComparison.Ordinal)).ToLower();
                clipTypeFolder = LibraryConfig.AudioObjectPath;
                clipType = ParseTypeAndPrepareDirectory(clipTypeString, ref clipTypeFolder);
                return clipName;
            }

            public static string[] ParseAssetTags(string assetName)
            {
                string[] tags = assetName.Split("-");
                return tags;
            }
        }

        #endregion

        #region CLIP SAMPLE DATA BLOB

        public static class AudioBlob
        {
            public static bool BuildBlob(EntityManager manager, AudioObject audioObject)
            {
                if (!audioObject.ValidClip)
                    return false;

                Entity audioBlobEntity = manager.CreateEntity();

                var clipData = new float[audioObject.SampleCount];
                audioObject.Clip.GetData(clipData, 0);

                using var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref FloatBlobAsset audioClipBlobAsset = ref blobBuilder.ConstructRoot<FloatBlobAsset>();

                BlobBuilderArray<float> sampleArray = blobBuilder.Allocate
                        (ref audioClipBlobAsset.Array, (audioObject.SampleCount / audioObject.Channels));
                GetAudioClipSampleBlob(ref sampleArray, clipData, audioObject.Channels);

                BlobAssetReference<FloatBlobAsset> audioClipBlobAssetRef
                        = blobBuilder.CreateBlobAssetReference<FloatBlobAsset>(Allocator.Persistent);
                manager.AddComponentData
                (audioBlobEntity, new AssetSampleArray {
                    AssetIndex = audioObject.ClipEntityIndex, SampleBlob = audioClipBlobAssetRef
                });

#if UNITY_EDITOR
                manager.SetName
                (audioBlobEntity,
                    LibraryConfig.AssetEntityPrefix + "." + audioObject.ClipEntityIndex + "." + audioObject.Clip.name);
#endif
                return true;
            }

            private static void GetAudioClipSampleBlob(
                ref BlobBuilderArray<float> audioBlob, IReadOnlyList<float> samples, int channels)
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
}