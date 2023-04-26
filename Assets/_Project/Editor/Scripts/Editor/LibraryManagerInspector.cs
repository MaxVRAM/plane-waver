using UnityEditor;
using UnityEngine;

using MaxVRAM.CustomGUI;

namespace PlaneWaver.Library
{
    [CustomEditor(typeof(LibraryManager))]
    public class AudioLibraryInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var library = (LibraryManager)target;
            
            MaxGUI.EditorUILine(Color.gray, 2, 20);

            // TODO: Change this preview function over to IPlayable window

            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Preview Clip"))
            {
                if (library.PreviewAudioObject != null && library.AudioSource != null)
                {
                    AudioObject audioObject = library.PreviewAudioObject;
                    // Debug.Log($"Previewing audio asset clip '{audioObject.Clip.name}' with duration {audioObject.Duration}.");
                    library.AudioSource.clip = audioObject.Clip;
                    library.AudioSource.Play();
                }
            }
            
            if (GUILayout.Button("Stop Preview"))
            {
                if (library.AudioSource != null && library.AudioSource.isPlaying)
                    library.AudioSource.Stop();
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
                Debug.Log($"Loaded {library.AudioObjects.Count} audio objects.");
            }

            if (GUILayout.Button("Rebuild Audio Library"))
            {
                library.AudioObjects = LibraryUtilities.BuildAudioObjects();
            }

            GUILayout.EndHorizontal();
        }
    }
}
