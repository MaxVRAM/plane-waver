using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PlaneWaver.Emitters
{
    public class AssetHandler
    {
        [OnOpenAsset]
        public static bool OpenEditor(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId) as VolatileEmitterObject;
            if (obj == null) return false;
            VolatileEmitterObjectEditorWindow.Open(obj);
            return true;
        }
    }
    
    [CustomEditor(typeof(VolatileEmitterObject))]
    public class VolatileEmitterObjectCustomEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Editor"))
            {
                VolatileEmitterObjectEditorWindow.Open((VolatileEmitterObject)target);
            }
        }
    }
}