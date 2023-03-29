using UnityEditor;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    [CustomPropertyDrawer(typeof(BaseEmitterAuth))]
    public class EmitterAuthDrawer : PropertyDrawer
    {
        private bool _expanded;
        protected SerializedProperty Emitter;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            const int margin = 2;
            const int buttonSize = 16;
            const float conditionWidth = 80;
            const float viewLargeWidth = 350;

            float viewWidth = EditorGUIUtility.currentViewWidth;
            float labelWidth = EditorGUIUtility.labelWidth;
            bool largeWindow = viewWidth > viewLargeWidth;
            
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
                x = labelRect.xMax - buttonSize - margin,
                width = buttonSize
            };

            _expanded = EditorGUI.Foldout(labelRect, _expanded, labelContent);
            EditorGUI.PropertyField(toggleRect, enabled, GUIContent.none);
            
            position = new Rect(position) {
                x = labelRect.xMax + margin,
                width = viewWidth - labelRect.xMax - margin
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
                var emitterRect = new Rect(position) {
                    x = position.x + margin,
                    y = position.y + EditorGUIUtility.singleLineHeight,
                    width = position.width - margin * 2
                };
                
                EditorGUI.indentLevel++;
                SerializedProperty reflect = property.FindPropertyRelative("ReflectPlayhead");
                EditorGUI.PropertyField(emitterRect, reflect, GUIContent.none);
                SerializedProperty volume = property.FindPropertyRelative("VolumeAdjustment");
                EditorGUI.PropertyField(emitterRect, volume, GUIContent.none);
                SerializedProperty attenuation = property.FindPropertyRelative("DynamicAttenuation");
                EditorGUI.PropertyField(emitterRect, attenuation, GUIContent.none);
                SerializedProperty runtime = property.FindPropertyRelative("RuntimeState");
                EditorGUI.PropertyField(emitterRect, runtime, GUIContent.none);
                EditorGUI.indentLevel--;
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
