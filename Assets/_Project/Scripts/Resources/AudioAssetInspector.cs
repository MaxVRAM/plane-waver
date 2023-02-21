using UnityEditor;
using UnityEngine;

namespace PlaneWaver
{
    [CustomEditor(typeof(AudioLibrary))]
    public class AudioAssetInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            AudioLibrary audioLibrary = (AudioLibrary)target;

            GUILayout.BeginHorizontal();

            // TODO: Move this preview function over to IPlayable window
            if (GUILayout.Button("Preview Sound"))
            {
                if (audioLibrary._PreviewAudioSource != null && audioLibrary.AudioAssets != null)
                {
                    AudioAssetScriptable audioAsset = audioLibrary.AudioAssets[0];
                    if (audioAsset != null)
                    {
                        Debug.Log($"Playing audio asset preview: {audioAsset.Clip.name}.");
                        audioLibrary._PreviewAudioSource.clip = audioAsset.Clip;
                        audioLibrary._PreviewAudioSource.Play();
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
                audioLibrary.BuildAudioAssets();
            }

            GUILayout.EndHorizontal();
        }
    }
}
