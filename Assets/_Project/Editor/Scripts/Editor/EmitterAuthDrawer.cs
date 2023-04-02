﻿using UnityEditor;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    [CustomPropertyDrawer(typeof(BaseEmitterAuth))]
    public class EmitterAuthDrawer : PropertyDrawer
    {
        private bool _expanded;
        private float _propertyHeight = EditorGUIUtility.singleLineHeight;
        protected SerializedProperty Emitter;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return _propertyHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            const int margin = 2;
            const int buttonSize = 16;
            const float conditionWidth = 80;
            const float viewLargeWidth = 350;

            float viewWidth = EditorGUIUtility.currentViewWidth;
            float labelWidth = EditorGUIUtility.labelWidth;
            bool largeWindow = viewWidth > viewLargeWidth;
            
            _propertyHeight = EditorGUIUtility.singleLineHeight;
            position.height = _propertyHeight;
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty enabled = property.FindPropertyRelative("Enabled");
            SerializedProperty condition = property.FindPropertyRelative("Condition");
            var emitterObject = (BaseEmitterObject) Emitter.objectReferenceValue;

            var labelContent = new GUIContent(
                Emitter.objectReferenceValue != null ? emitterObject.EmitterName : "Unassigned emitter");
                    
            var labelRect = new Rect(position) {
                x = position.x + margin,
                width = labelWidth
            };
            var toggleRect = new Rect(position) {
                x = labelRect.xMax - buttonSize - margin * 3,
                width = buttonSize
            };

            _expanded = EditorGUI.Foldout(labelRect, _expanded, labelContent);
            EditorGUI.PropertyField(toggleRect, enabled, GUIContent.none);
            
            position = new Rect(position) {
                x = labelRect.xMax - margin,
                width = viewWidth - labelRect.xMax - margin * 2
            };
            
            Rect objectRect = largeWindow
                    ? new Rect(position) { width = position.width - conditionWidth - margin * 2 }
                    : new Rect(position) { width = position.width - margin };
                    
            
            EditorGUI.PropertyField(objectRect, Emitter, GUIContent.none);

            if (largeWindow)
            {
                var conditionRect = new Rect(position) {
                    x = objectRect.xMax + margin,
                    width = conditionWidth
                };
                EditorGUI.PropertyField(conditionRect, condition, GUIContent.none);
            }
            
            if (_expanded && Emitter.objectReferenceValue != null)
            {
                position.x = labelRect.x;
                position.y += EditorGUIUtility.singleLineHeight + margin;
                position.width = viewWidth - margin * 4 - labelRect.x;
                
                var emitterRect = new Rect(position) {
                    width = position.width - margin * 2
                };
                
                SerializedProperty reflect = property.FindPropertyRelative("ReflectPlayhead");
                SerializedProperty volume = property.FindPropertyRelative("VolumeAdjustment");
                SerializedProperty attenuation = property.FindPropertyRelative("DynamicAttenuation");
                SerializedProperty runtime = property.FindPropertyRelative("RuntimeState");
                
                float reflectHeight = GetPropertyHeight(reflect, GUIContent.none);
                float volumeHeight = GetPropertyHeight(volume, GUIContent.none);
                float attenuationHeight = GetPropertyHeight(attenuation, GUIContent.none);
                float runtimeHeight = GetPropertyHeight(runtime, GUIContent.none);
                
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(emitterRect, reflect);
                emitterRect.y += reflectHeight;
                EditorGUI.PropertyField(emitterRect, volume);
                emitterRect.y += volumeHeight;
                EditorGUI.PropertyField(emitterRect, attenuation);
                emitterRect.y += attenuationHeight;
                EditorGUI.PropertyField(emitterRect, runtime);
                EditorGUI.indentLevel--;
                
                _propertyHeight += reflectHeight + volumeHeight + attenuationHeight + runtimeHeight + margin;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(StableEmitterAuth))]
    public class StableAuthDrawer : EmitterAuthDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Emitter = property.FindPropertyRelative("StableEmitterAsset");
            base.OnGUI(position, property, label);
        }
    }
    
    [CustomPropertyDrawer(typeof(VolatileEmitterAuth))]
    public class VolatileAuthDrawer : EmitterAuthDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Emitter = property.FindPropertyRelative("VolatileEmitterAsset");
            base.OnGUI(position, property, label);
        }
    }
    
}
