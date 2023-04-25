using UnityEditor;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    [CustomPropertyDrawer(typeof(BaseEmitterAuth))]
    public class EmitterAuthDrawer : PropertyDrawer
    {
        private float _propertyHeight = EditorGUIUtility.singleLineHeight;
        protected SerializedProperty Emitter;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return _propertyHeight;
            // return EditorGUI.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int indent = EditorGUI.indentLevel;
            Rect originalPropertyRect = position;
            _propertyHeight = EditorGUIUtility.singleLineHeight;
            position.height = _propertyHeight;

            const int buttonSize = 16;
            const int margin = 2;
            float viewWidth = EditorGUIUtility.currentViewWidth;
            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = viewWidth - labelWidth;
            bool expanded = property.isExpanded;


            var emitterObject = (BaseEmitterObject)Emitter.objectReferenceValue;
            SerializedProperty enabled = property.FindPropertyRelative("Enabled");

            // EditorGUI.BeginProperty(position, label, property);
            label = Emitter.objectReferenceValue != null
                    ? new GUIContent { text = emitterObject.EmitterName }
                    : new GUIContent { text = "Unassigned emitter" };

            position.width = labelWidth;
            EditorGUI.BeginChangeCheck();
            EditorGUI.Foldout(position, property.isExpanded, label);

            if (EditorGUI.EndChangeCheck())
            {
                property.isExpanded = !property.isExpanded;
                expanded = property.isExpanded;
            }

            position.x = position.xMax + margin;
            position.width = buttonSize;
            EditorGUI.PropertyField(position, enabled, GUIContent.none);

            // EditorGUI.indentLevel = 0;

            // position = new Rect(position) {
            //     x = labelRect.xMax - margin * 2,
            //     width = viewWidth - labelRect.xMax - margin * 4
            // };

            position.x = position.xMax + margin;
            position.width = originalPropertyRect.xMax - position.x;
            EditorGUI.PropertyField(position, Emitter, GUIContent.none);

            // Rect objectRect = largeWindow
            //         ? new Rect(position) { width = position.width - conditionWidth - margin * 2 }
            //         : new Rect(position) { width = position.width - margin };


            // if (largeWindow)
            // {
            //     var conditionRect = new Rect(position) {
            //         x = objectRect.xMax + margin,
            //         width = conditionWidth
            //     };
            //
            //     EditorGUI.PropertyField(conditionRect, condition, GUIContent.none);
            // }

            if (expanded && Emitter.objectReferenceValue != null)
            {
                position.x = originalPropertyRect.x;
                position.width = originalPropertyRect.width;
                EditorGUI.indentLevel++;
                // position.width = viewWidth - labelRect.x;
                // position = EditorGUI.IndentedRect(position);
                //position.width = originalPropertyRect.width - position.x;

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

                float conditionHeight = EditorGUI.GetPropertyHeight(condition);
                float reflectHeight = EditorGUI.GetPropertyHeight(reflect);
                float attenuationHeight = EditorGUI.GetPropertyHeight(attenuation, attenuation.isExpanded);
                float runtimeHeight = EditorGUI.GetPropertyHeight(runtime, runtime.isExpanded);

                position.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, condition, new GUIContent { text = "Play Condition" });
                position.y += conditionHeight;
                EditorGUI.PropertyField(position, reflect, new GUIContent { text = "Reflect Playhead" });
                position.y += reflectHeight;
                EditorGUI.PropertyField(position, attenuation, attenuation.isExpanded);
                position.y += attenuationHeight;
                EditorGUI.PropertyField(position, runtime,new GUIContent { text = "Runtime State" }, runtime.isExpanded);
                position.y += runtimeHeight;

                _propertyHeight = position.y - originalPropertyRect.y;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            EditorGUI.indentLevel = indent;
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