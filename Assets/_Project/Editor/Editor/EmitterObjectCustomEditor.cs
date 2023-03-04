using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using PlaneWaver.Emitters;
using PlaneWaver.Library;
using UnityEngine.UIElements;

namespace PlaneWaver.Parameters
{
    public class AssetHandler
    {
        [OnOpenAsset]
        public static bool OpenEditor(int instanceId, int line)
        {
            switch (EditorUtility.InstanceIDToObject(instanceId))
            {
                case StableEmitterObject emitter:
                    EmitterObjectEditorWindow.Open(emitter);
                    return true;
                case VolatileEmitterObject emitter:
                    EmitterObjectEditorWindow.Open(emitter);
                    return true;
                default:
                    return false;
            }
        }
    }

    [CustomEditor(typeof(EmitterObject))]
    public class EmitterObjectCustomEditor : Editor
    {
        
        public override void OnInspectorGUI()
        {
            var emitter = (EmitterObject)target;
            
            EditorGUILayout.TextField("Emitter Name", ((EmitterObject)target).EmitterName);
            EditorGUILayout.TextField("Description", ((EmitterObject)target).Description);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.PrefixLabel("Audio Object", new StyleBackground()  EditorStyles.boldLabel);
            EditorGUILayout.PrefixLabel("Audio Object");
            EditorGUILayout.ObjectField(((EmitterObject)target).AudioObject, typeof(AudioObject),false);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            foreach (Parameter parameter in emitter.Parameters)
            {
                if (GUILayout.Button(parameter.ParameterProperties.Name))
                {
                    Debug.Log($"{parameter.ParameterProperties.Name} - {parameter.ParameterProperties.ParameterMaxRange}");
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    
    [CustomEditor(typeof(StableEmitterObject))]
    public class StableEmitterObjectCustomEditor : EmitterObjectCustomEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var stableEmitter = (StableEmitterObject)target;
            
            if (GUILayout.Button("Open Editor"))
                EmitterObjectEditorWindow.Open(stableEmitter);            
        }
    }
    
    [CustomEditor(typeof(VolatileEmitterObject))]
    public class VolatileEmitterObjectCustomEditor : EmitterObjectCustomEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var volatileEmitter = (VolatileEmitterObject)target;
            
            if (GUILayout.Button("Open Editor"))
                EmitterObjectEditorWindow.Open(volatileEmitter);
        }
    }
}