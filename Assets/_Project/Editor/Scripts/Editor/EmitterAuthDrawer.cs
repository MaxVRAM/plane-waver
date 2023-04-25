using UnityEditor;
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
            // return EditorGUI.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // const int margin = 2;
            const int buttonSize = 16;
            // const float conditionWidth = 80;
            // const float viewLargeWidth = 350;

            float viewWidth = EditorGUIUtility.currentViewWidth;
            float labelWidth = EditorGUIUtility.labelWidth;
            // bool largeWindow = viewWidth > viewLargeWidth;

            Rect originalPropertyRect = position;
            _propertyHeight = EditorGUIUtility.singleLineHeight;
            position.height = _propertyHeight;
            int indent = EditorGUI.indentLevel;

            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty enabled = property.FindPropertyRelative("Enabled");
            var emitterObject = (BaseEmitterObject)Emitter.objectReferenceValue;

            var labelContent = new GUIContent(
                Emitter.objectReferenceValue != null
                        ? emitterObject.EmitterName
                        : "Unassigned emitter");

            position.width = labelWidth;
            _expanded = EditorGUI.Foldout(position, _expanded, labelContent);

            position.x = position.xMax;
            position.width = buttonSize;
            EditorGUI.PropertyField(position, enabled, GUIContent.none);

            // EditorGUI.indentLevel = 0;

            // position = new Rect(position) {
            //     x = labelRect.xMax - margin * 2,
            //     width = viewWidth - labelRect.xMax - margin * 4
            // };

            position.x = position.xMax;
            position.width = originalPropertyRect.width - position.x;

            // Rect objectRect = largeWindow
            //         ? new Rect(position) { width = position.width - conditionWidth - margin * 2 }
            //         : new Rect(position) { width = position.width - margin };

            EditorGUI.PropertyField(position, Emitter, GUIContent.none);

            // if (largeWindow)
            // {
            //     var conditionRect = new Rect(position) {
            //         x = objectRect.xMax + margin,
            //         width = conditionWidth
            //     };
            //
            //     EditorGUI.PropertyField(conditionRect, condition, GUIContent.none);
            // }

            if (_expanded && Emitter.objectReferenceValue != null)
            {
                position.y += EditorGUIUtility.singleLineHeight;
                position.x = originalPropertyRect.x;
                // position.width = viewWidth - labelRect.x;
                position = EditorGUI.IndentedRect(position);

                // position.x = labelRect.x - margin;
                // position.y += EditorGUIUtility.singleLineHeight;
                // position.width = viewWidth - labelRect.x - margin;

                // var fieldRect = new Rect(position) {
                //     width = position.width
                // };

                SerializedProperty condition = property.FindPropertyRelative("Condition");
                SerializedProperty reflect = property.FindPropertyRelative("ReflectPlayhead");
                SerializedProperty attenuation = property.FindPropertyRelative("Attenuation");
                SerializedProperty runtime = property.FindPropertyRelative("RuntimeState");

                float conditionHeight = EditorGUI.GetPropertyHeight(condition, GUIContent.none);
                float reflectHeight = EditorGUI.GetPropertyHeight(reflect, GUIContent.none);
                float attenuationHeight = EditorGUI.GetPropertyHeight(attenuation, GUIContent.none, attenuation.isExpanded);
                float runtimeHeight = EditorGUI.GetPropertyHeight(runtime, GUIContent.none, attenuation.isExpanded);

                EditorGUI.PropertyField(position, condition,  new GUIContent { text = "Play Condition" });
                position.y += conditionHeight;
                EditorGUI.PropertyField(position, reflect, new GUIContent { text = "Reflect Playhead" });
                position.y += reflectHeight;
                EditorGUI.PropertyField(position, attenuation, new GUIContent { text = "Attenuation" });
                position.y += attenuationHeight;
                EditorGUI.PropertyField(position, runtime, new GUIContent { text = "Runtime State" });
                position.y += runtimeHeight;

                // _propertyHeight = position.yMax - originalPropertyRect.y; // - position.y;
                _propertyHeight = position.height; // - originalPropertyRect.y; // - position.y;
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