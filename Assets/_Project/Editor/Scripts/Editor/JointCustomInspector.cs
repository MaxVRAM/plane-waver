using UnityEditor;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    [CustomEditor(typeof(Joint))]
    public class JointCustomInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            // TODO - Detect joint type of target and create instance of associated JointBaseObject type

            
            if (GUILayout.Button("Create Joint Asset"))
            {
                var joint = (Joint)target;
                var jointObject = CreateInstance<JointBaseObject>();
                string jointTypeName = joint.GetType().Name.Replace("Joint", "");
                jointObject.name = $"Joint.{jointTypeName}.new";
                AssetDatabase.CreateAsset(jointObject, $"Assets/_Project/Resources/Joints/{jointObject.name}.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorGUIUtility.PingObject(jointObject);
            }
            
            EditorGUILayout.Space(3);
            
            DrawDefaultInspector();
        }
    }
}