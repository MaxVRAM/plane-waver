using UnityEditor;
using UnityEngine;

using MaxVRAM.GUI;
using PlaneWaver.Library;

namespace PlaneWaver
{
    [CustomEditor(typeof(AudioLibrary))]
    public class AudioLibraryAssetInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var library = (AudioLibrary)target;
            
            // TODO: Change this preview function over to IPlayable window
            GUILayout.BeginHorizontal();

            
            // BUG - Disabled this while debugging issue with audio system disabling itself.
            
            if (GUILayout.Button("Preview Clip"))
            {
                // if (library.PreviewAudioObject != null || library.PreviewAudioSource != null)
                // {
                //     AudioObject audioObject = library.PreviewAudioObject;
                //     Debug.Log($"Previewing audio asset clip '{audioObject.Clip.name}' with duration {audioObject.Duration}.");
                //     library.PreviewAudioSource.clip = audioObject.Clip;
                //     library.PreviewAudioSource.Play();
                // }
            }
            
            if (GUILayout.Button("Stop Preview"))
            {
                // if (library.PreviewAudioSource != null && library.PreviewAudioSource.isPlaying)
                //     library.PreviewAudioSource.Stop();
            }

            GUILayout.EndHorizontal();
            
            MaxGUI.EditorUILine(Color.gray, 2, 20);
            
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Create Stable Emitter"))
            {
                AudioLibrary.CreateEmitterAsset(library.PreviewAudioObject, false);
            }

            if (GUILayout.Button("Create Volatile Emitter"))
            {
                AudioLibrary.CreateEmitterAsset(library.PreviewAudioObject, true);
            }

            GUILayout.EndHorizontal();
            
            MaxGUI.EditorUILine(Color.gray, 2, 20);
            
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reload Audio Objects"))
            {
                library.LoadAssets();
            }

            if (GUILayout.Button("Rebuild Audio Library"))
            {
                library.BuildAudioObjects();
            }

            GUILayout.EndHorizontal();
        }
    }
}
