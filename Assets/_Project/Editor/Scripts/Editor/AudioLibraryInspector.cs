using UnityEditor;
using UnityEngine;

using MaxVRAM.GUI;
using PlaneWaver.Library;

namespace PlaneWaver.Library
{
    [CustomEditor(typeof(AudioLibrary))]
    public class AudioLibraryInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var library = (AudioLibrary)target;
            
            MaxGUI.EditorUILine(Color.gray, 2, 20);

            // TODO: Change this preview function over to IPlayable window

            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Preview Clip"))
            {
                if (library.PreviewAudioObject != null || library.PreviewAudioSource != null)
                {
                    AudioObject audioObject = library.PreviewAudioObject;
                    Debug.Log($"Previewing audio asset clip '{audioObject.Clip.name}' with duration {audioObject.Duration}.");
                    library.PreviewAudioSource.clip = audioObject.Clip;
                    library.PreviewAudioSource.Play();
                }
            }
            
            if (GUILayout.Button("Stop Preview"))
            {
                if (library.PreviewAudioSource != null && library.PreviewAudioSource.isPlaying)
                    library.PreviewAudioSource.Stop();
            }

            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Create Stable Emitter"))
            {
                LibraryUtilities.CreateEmitterAsset(library.PreviewAudioObject, false);
            }

            if (GUILayout.Button("Create Volatile Emitter"))
            {
                LibraryUtilities.CreateEmitterAsset(library.PreviewAudioObject, true);
            }

            GUILayout.EndHorizontal();
            
            MaxGUI.EditorUILine(Color.gray, 2, 20);
            
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reload Audio Objects"))
            {
                library.AudioObjects = LibraryUtilities.LoadAssets();
            }

            if (GUILayout.Button("Rebuild Audio Library"))
            {
                library.AudioObjects = LibraryUtilities.BuildAudioObjects();
            }

            GUILayout.EndHorizontal();
        }
    }
}
