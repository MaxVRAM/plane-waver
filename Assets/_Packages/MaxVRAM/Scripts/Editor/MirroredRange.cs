using MaxVRAM.Extensions;
using UnityEditor;
using UnityEngine;

namespace MaxVRAM.CustomGUI
{
    [CustomPropertyDrawer(typeof(MirroredRangeAttribute))]
    public class MirroredRange : FancyPropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            label = EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.Vector2)
            {
                EditorGUI.LabelField(position, label.text, "Use MirroredRange with Vector2.");
                return;
            }

            var attr = (MirroredRangeAttribute)attribute;
            int digits = attr.Digits;
            float fieldWidth = attr.FieldWidth;

            position = EditorGUI.PrefixLabel(position, label);

            int numberOfFields = EditorGUIUtility.currentViewWidth < 320 ? 1 : 2;
            float sliderWidth = position.width - numberOfFields * fieldWidth - numberOfFields * Margin;
            GUIStyle primarySliderStyle = GUI.skin.horizontalSlider;
            
            var fieldRect = new Rect(position) {
                width = fieldWidth
            };
            
            var sliderRect = new Rect(position) {
                x = fieldRect.xMax + Margin,
                width = sliderWidth
            };
            
            var secondFieldRect = new Rect(fieldRect) {
                x = sliderRect.xMax + Margin
            };
            
            Vector2 vectorValue = property.vector2Value;
            float xValue = vectorValue.x;
            float mirroredValue = Mathf.Clamp(vectorValue.y, attr.Min, attr.Max);

            EditorGUI.BeginChangeCheck();
            float fieldValue = EditorGUI.FloatField(fieldRect, GUIContent.none, xValue.LimitDigits(digits));
            if (EditorGUI.EndChangeCheck())
                xValue = Mathf.Clamp(fieldValue, attr.Min, attr.Max);

            if (sliderRect.width > 40)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUI.HorizontalSlider(sliderRect, mirroredValue, attr.Min, attr.Max);
                EditorGUI.EndDisabledGroup();     
                primarySliderStyle = GUIStyle.none;
            }

            var centreRect = new Rect(sliderRect) {
                x = sliderRect.x + sliderRect.width / 2,
                width = 1,
                y = sliderRect.y + sliderRect.height * 0.25f,
                height = sliderRect.height * 0.5f
            };
            EditorGUI.DrawRect(centreRect, Color.gray);

            float sliderValue = GUI.HorizontalSlider
                    (sliderRect, xValue, attr.Min, attr.Max, primarySliderStyle, GUI.skin.horizontalSliderThumb);
            if (EditorGUI.EndChangeCheck())
                xValue = sliderValue;

            if (numberOfFields == 2)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.FloatField(secondFieldRect, GUIContent.none, mirroredValue.LimitDigits(digits));
                EditorGUI.EndDisabledGroup();
            }

            float rangeCentre = MaxMath.RangeCentre(attr.Min, attr.Max);

            if (CheckResetButton(attr.ShowResetButton, position))
                xValue = rangeCentre;

            property.vector2Value = xValue.MirroredValueVector(rangeCentre);
            EditorGUI.EndProperty();
        }
    }

    public class MirroredRangeAttribute : PropertyAttribute
    {
        public readonly float Min;
        public readonly float Max;
        public readonly bool ShowResetButton;
        public readonly int Digits;
        public readonly float FieldWidth;

        public MirroredRangeAttribute(
            float min, float max, bool showResetButton = true, int digits = 5, float fieldWidth = 50)
        {
            Min = min;
            Max = max;
            ShowResetButton = showResetButton;
            Digits = digits;
            FieldWidth = fieldWidth;
        }
    }
}