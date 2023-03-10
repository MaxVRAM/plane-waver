using UnityEditor;
using UnityEngine;

using PlaneWaver.Emitters;

namespace PlaneWaver.Modulation
{
    [CustomPropertyDrawer(typeof(Parameter))]
    public class EmitterParameterCustomDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int indent = EditorGUI.indentLevel;
            
            bool isVolatile = (BaseEmitterObject)property.serializedObject.targetObject is VolatileEmitter;
            SerializedProperty parameterProperties = property.FindPropertyRelative("ParameterProperties");
            SerializedProperty modulationInput = property.FindPropertyRelative("ModulationInput");
            SerializedProperty modulationData = property.FindPropertyRelative("ModulationData");
            
            label = new GUIContent(parameterProperties.FindPropertyRelative("Name").stringValue);
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.indentLevel = 0;
            
            EditorGUI.LabelField(position, label, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(modulationInput);
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}