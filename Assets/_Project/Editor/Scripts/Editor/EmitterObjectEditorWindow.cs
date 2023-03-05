using System;
using UnityEditor;
using PlaneWaver.Emitters;
using PlaneWaver.GUI;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    public class EmitterObjectEditorWindow : ExtendedEditorWindow
    {
        // [MenuItem("Window/Modulation Data Editor")]
        // public static void ShowWindow()
        // {
        //     GetWindow<ModulationEditorWindow>("Modulation Data Editor");
        // }
        
        public static void Open(BaseEmitterObject emitter)
        {
            var window = GetWindow<EmitterObjectEditorWindow>("Emitter Object Editor");
            var serializedObject = new SerializedObject(emitter);
            window.SerialisedObject = serializedObject;
            Debug.Log($"Opening Emitter: {emitter.name}");
            
            var i = 0;
            SerializedProperty parameter = serializedObject.FindProperty("Parameters");
            //SerializedProperty property = serializedObject.GetIterator();
            while (i < 10)
            {
                Debug.Log($"Property: {parameter.name}");
                i++;
                if (!parameter.NextVisible(true))
                    break;
            }
        }

        public void OnGUI()
        {
            // CurrentProperty = SerialisedObject.FindProperty("Parameters");
            // DrawProperties(CurrentProperty, true);
        }
    }
}