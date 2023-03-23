using System;
using UnityEditor;
using UnityEngine;

using MaxVRAM.CustomGUI;

namespace PlaneWaver.Interaction
{
    [CustomEditor(typeof(Joint))]
    public class JointCustomInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(3);
            MaxGUI.EditorUILine(Color.gray);
            EditorGUILayout.Space(3);
            
            if (GUILayout.Button("Create Joint Asset"))
            {
                var joint = (Joint)target;
                Type jointObjectType = BaseJointObject.ComponentToJointObjectType(joint);
                string jointTypeName = joint.GetType().Name.Replace("Joint", "");
                var jointObject = (BaseJointObject)CreateInstance(jointObjectType);
                jointObject.name = $"jnt_{jointTypeName}";
                jointObject.StoreJointDataFromComponent(joint);
                AssetDatabase.CreateAsset(jointObject, $"Assets/_Project/Resources/Joints/{jointObject.name}.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorGUIUtility.PingObject(jointObject);
            }
            
            EditorGUILayout.Space(3);
            MaxGUI.EditorUILine(Color.gray);
            EditorGUILayout.Space(3);
            DrawDefaultInspector();

        }
    }
    
    [CustomEditor(typeof(HingeJoint))]
    public class HingeJointObjectCustomEditor : JointCustomInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
    
    [CustomEditor(typeof(FixedJoint))]
    public class FixedJointObjectCustomEditor : JointCustomInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
    
    [CustomEditor(typeof(SpringJoint))]
    public class SpringJointObjectCustomEditor : JointCustomInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
    
    [CustomEditor(typeof(CharacterJoint))]
    public class CharacterJointObjectCustomEditor : JointCustomInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
    
    [CustomEditor(typeof(ConfigurableJoint))]
    public class ConfigurableJointObjectCustomEditor : JointCustomInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}
