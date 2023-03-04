using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using PlaneWaver.Emitters;

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
        // public override void OnInspectorGUI()
        // {
        //     if (GUILayout.Button("Open Editor"))
        //     {
        //         EmitterObjectEditorWindow.Open((EmitterObject)target);
        //     }
        // }
    }
    
    [CustomEditor(typeof(StableEmitterObject))]
    public class StableEmitterObjectCustomEditor : EmitterObjectCustomEditor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Editor"))
            {
                EmitterObjectEditorWindow.Open((StableEmitterObject)target);
            }
        }
    }
    
    [CustomEditor(typeof(VolatileEmitterObject))]
    public class VolatileEmitterObjectCustomEditor : EmitterObjectCustomEditor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Editor"))
            {
                EmitterObjectEditorWindow.Open((VolatileEmitterObject)target);
            }
        }
    }
}