using UnityEditor;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    [CustomPropertyDrawer(typeof(EmitterAttenuator))]
    public class EmitterAttenuatorDrawer : PropertyDrawer
    {
        private float _propertyHeight = EditorGUIUtility.singleLineHeight;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return _propertyHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int indent = EditorGUI.indentLevel;
            Rect originalPropertyRect = position;
            _propertyHeight = EditorGUIUtility.singleLineHeight;
            position.height = _propertyHeight;
            
            const int buttonSize = 12;
            const int margin = 2;
            float labelWidth = EditorGUIUtility.labelWidth;
            bool expanded = property.isExpanded;
            
            SerializedProperty gain = property.FindPropertyRelative("Gain");
            SerializedProperty radius = property.FindPropertyRelative("AudibleRange");
            SerializedProperty ageFadeIn = property.FindPropertyRelative("AgeFadeIn");
            SerializedProperty ageFadeOut = property.FindPropertyRelative("AgeFadeOut");
            SerializedProperty fadeState = property.FindPropertyRelative("ConnectFadeIn");
            SerializedProperty connectFadeTime = property.FindPropertyRelative("ConnectFadeDuration");
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.Foldout(position, property.isExpanded, label);
            if (EditorGUI.EndChangeCheck())
            {
                property.isExpanded = !property.isExpanded;
                expanded = property.isExpanded;
            }

            EditorGUI.indentLevel = 0;
            position.x = originalPropertyRect.x + labelWidth + margin;
            position.width = originalPropertyRect.xMax - position.x;
            EditorGUI.PropertyField(position, gain, GUIContent.none, false);

            if (expanded)
            {
                position = originalPropertyRect;
                EditorGUI.indentLevel++;
                EditorGUI.indentLevel++;
                
                position.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, radius);
                position.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, ageFadeIn);
                position.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, ageFadeOut);
                
                bool fadeStateValue = fadeState.boolValue;
                position.y += EditorGUIUtility.singleLineHeight;
                position.width = labelWidth + buttonSize;
                
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, fadeState);
                if (EditorGUI.EndChangeCheck())
                {
                    fadeStateValue = fadeState.boolValue;
                }
                
                position.x = originalPropertyRect.x + labelWidth + buttonSize;
                position.xMax = originalPropertyRect.xMax;
                using (new EditorGUI.DisabledScope(!fadeStateValue))
                {
                    EditorGUI.PropertyField(position, connectFadeTime, GUIContent.none);
                }
                position.y += EditorGUIUtility.singleLineHeight;
                
                _propertyHeight = position.y - originalPropertyRect.y;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            EditorGUI.indentLevel = indent;
        }
    }
}