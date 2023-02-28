using UnityEditor;
using UnityEngine;

using PlaneWaver.Library;

namespace PlaneWaver
{
    [CustomEditor(typeof(AssetLibrary))]
    public class AudioAssetInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            AssetLibrary audioLibrary = (AssetLibrary)target;

            GUILayout.BeginHorizontal();

            // TODO: Move this preview function over to IPlayable window
            if (GUILayout.Button("Preview Sound"))
            {
                if (audioLibrary.PreviewAudioSource != null && audioLibrary.AudioAssets != null)
                {
                    AudioAsset audioAsset = audioLibrary.AudioAssets[0];
                    if (audioAsset != null)
                    {
                        Debug.Log($"Playing audio asset preview: {audioAsset.Clip.name}.");
                        audioLibrary.PreviewAudioSource.clip = audioAsset.Clip;
                        audioLibrary.PreviewAudioSource.Play();
                    }
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reload Audio Assets"))
            {
                Debug.Log("Todo: create reload function.");
                audioLibrary.ReloadAudioAssets();
            }

            if (GUILayout.Button("Rebuild Audio Assets"))
            {
                audioLibrary.RebuildAudioAssets();
            }

            GUILayout.EndHorizontal();
        }
    }
}
