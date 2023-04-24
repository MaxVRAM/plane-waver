using UnityEditor;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    [CustomPropertyDrawer(typeof(EmitterAttenuator))]
    public class EmitterAttenuatorDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight * 5;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
            // return EditorGUI.GetPropertyHeight(property, label);
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty radius = property.FindPropertyRelative("RadiusMultiplier");
            SerializedProperty ageFadeIn = property.FindPropertyRelative("AgeFadeIn");
            SerializedProperty ageFadeOut = property.FindPropertyRelative("AgeFadeOut");
            SerializedProperty fadeState = property.FindPropertyRelative("ConnectionFade");
            SerializedProperty connectFadeTime = property.FindPropertyRelative("ConnectFadeIn");
            
            EditorGUI.PropertyField(position, radius);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, ageFadeIn);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, ageFadeOut);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, fadeState);
            position.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, connectFadeTime);
            
            
        }
    }
}