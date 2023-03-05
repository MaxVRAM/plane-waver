using UnityEditor;
using UnityEngine;

namespace PlaneWaver.Parameters
{
    public class ParameterDrawer : PropertyDrawer
    {
        [CustomPropertyDrawer(typeof(Parameter))]
        public class EmitterObjectCustomEditor : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);
                
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
                
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                
                
                EditorGUI.EndProperty();
            }
        }
    }
}