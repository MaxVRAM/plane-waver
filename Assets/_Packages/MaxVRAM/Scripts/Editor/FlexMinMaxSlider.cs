// https://frarees.github.io/default-gist-license

using System;
using UnityEngine;
using UnityEditor;

namespace MaxVRAM.GUI
{
    [CustomPropertyDrawer(typeof(FlexMinMaxSliderAttribute))]
    internal class FlexMinMaxSliderDrawer : PropertyDrawer
    {
        private const string KVectorMinName = "x";
        private const string KVectorMaxName = "y";
        private const float KFloatFieldWidth = 16f;
        private const float KSpacing = 2f;
        private const float KRoundingValue = 100f;

        private static readonly int ControlHash = "Foldout".GetHashCode();
        private static readonly GUIContent Unsupported = EditorGUIUtility.TrTextContent("Unsupported field type");

        private bool _pressed;
        private float _pressedMin;
        private float _pressedMax;

        private static float Round(float value, float roundingValue)
        {
            return roundingValue == 0 ? value : Mathf.Round(value * roundingValue) / roundingValue;
        }

        private float FlexibleFloatFieldWidth(float min, float max)
        {
            float n = Mathf.Max(Mathf.Abs(min), Mathf.Abs(max));
            return 14f + (Mathf.Floor(Mathf.Log10(Mathf.Abs(n)) + 1) * 2.5f);
        }

        private void SetVectorValue(SerializedProperty property, ref float min, ref float max, bool round)
        {
            if (!_pressed || (_pressed && !Mathf.Approximately(min, _pressedMin)))
            {
                using SerializedProperty x = property.FindPropertyRelative(KVectorMinName);
                SetValue(x, ref min, round);
            }

            if (!_pressed || (_pressed && !Mathf.Approximately(max, _pressedMax)))
            {
                using SerializedProperty y = property.FindPropertyRelative(KVectorMaxName);
                SetValue(y, ref max, round);
            }
        }

        private static void SetValue(SerializedProperty property, ref float v, bool round)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    property.floatValue = round ? Round(v, KRoundingValue) : v;
                    break;
                case SerializedPropertyType.Integer:
                    property.intValue = Mathf.RoundToInt(v);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float min, max;

            label = EditorGUI.BeginProperty(position, label, property);

            switch (property.propertyType)
            {
                case SerializedPropertyType.Vector2:
                {
                    Vector2 v = property.vector2Value;
                    min = v.x;
                    max = v.y;
                }
                    break;
                case SerializedPropertyType.Vector2Int:
                {
                    Vector2Int v = property.vector2IntValue;
                    min = v.x;
                    max = v.y;
                }
                    break;
                default:
                    EditorGUI.LabelField(position, label, Unsupported);
                    return;
            }

            if (attribute is not FlexMinMaxSliderAttribute attr) { return; }

            float ppp = EditorGUIUtility.pixelsPerPoint;
            float spacing = KSpacing * ppp;
            float fieldWidth = ppp *
                               (attr.DataFields && attr.FlexibleFields
                                       ? FlexibleFloatFieldWidth(attr.Min, attr.Max)
                                       : KFloatFieldWidth);

            int indent = EditorGUI.indentLevel;

            int id = GUIUtility.GetControlID(ControlHash, FocusType.Keyboard, position);
            Rect r = EditorGUI.PrefixLabel(position, id, label);

            Rect sliderPos = r;

            if (attr.DataFields)
            {
                sliderPos.x += fieldWidth + spacing;
                sliderPos.width -= (fieldWidth + spacing) * 2;
            }

            if (Event.current.type == EventType.MouseDown && sliderPos.Contains(Event.current.mousePosition))
            {
                _pressed = true;
                min = Mathf.Clamp(min, attr.Min, attr.Max);
                max = Mathf.Clamp(max, attr.Min, attr.Max);
                _pressedMin = min;
                _pressedMax = max;
                SetVectorValue(property, ref min, ref max, attr.Round);
                GUIUtility.keyboardControl = 0; // TODO keep focus but stop editing
            }

            if (_pressed && Event.current.type == EventType.MouseUp)
            {
                if (attr.Round) { SetVectorValue(property, ref min, ref max, true); }

                _pressed = false;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel = 0;
            EditorGUI.MinMaxSlider(sliderPos, ref min, ref max, attr.Min, attr.Max);
            EditorGUI.indentLevel = indent;

            if (EditorGUI.EndChangeCheck()) { SetVectorValue(property, ref min, ref max, false); }

            if (attr.DataFields)
            {
                Rect minPos = r;
                minPos.width = fieldWidth;

                SerializedProperty vectorMinProp = property.FindPropertyRelative(KVectorMinName);
                EditorGUI.showMixedValue = vectorMinProp.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel = 0;
                min = EditorGUI.DelayedFloatField(minPos, min);
                EditorGUI.indentLevel = indent;

                if (EditorGUI.EndChangeCheck())
                {
                    if (attr.Bound)
                    {
                        min = Mathf.Max(min, attr.Min);
                        min = Mathf.Min(min, max);
                    }

                    SetVectorValue(property, ref min, ref max, attr.Round);
                }

                vectorMinProp.Dispose();

                Rect maxPos = position;
                maxPos.x += maxPos.width - fieldWidth;
                maxPos.width = fieldWidth;

                SerializedProperty vectorMaxProp = property.FindPropertyRelative(KVectorMaxName);
                EditorGUI.showMixedValue = vectorMaxProp.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel = 0;
                max = EditorGUI.DelayedFloatField(maxPos, max);
                EditorGUI.indentLevel = indent;

                if (EditorGUI.EndChangeCheck())
                {
                    if (attr.Bound)
                    {
                        max = Mathf.Min(max, attr.Max);
                        max = Mathf.Max(max, min);
                    }

                    SetVectorValue(property, ref min, ref max, attr.Round);
                }

                vectorMaxProp.Dispose();

                EditorGUI.showMixedValue = false;
            }

            EditorGUI.EndProperty();
        }
    }
}