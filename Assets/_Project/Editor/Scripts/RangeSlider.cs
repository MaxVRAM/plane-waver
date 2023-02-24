using UnityEditor;
using UnityEngine;

namespace MaxVRAM.GUI
{
    [CustomPropertyDrawer(typeof(RangeSliderAttribute))]
    public class RangeSlider : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            RangeSliderAttribute rangeAttribute = (RangeSliderAttribute)attribute;
            SerializedPropertyType propertyType = property.propertyType;

            label.tooltip = rangeAttribute.Min.ToString("F2") + " to " + rangeAttribute.Max.ToString("F2");
            Rect controlRect = EditorGUI.PrefixLabel(position, label);
            Rect[] splittedRect = SplitRect(controlRect, 3);

            EditorGUI.BeginChangeCheck();

            if (propertyType == SerializedPropertyType.Vector2)
            {
                Vector2 sliderValues = property.vector2Value;
                EditorGUI.MinMaxSlider(splittedRect[1], ref sliderValues.x, ref sliderValues.y, rangeAttribute.Min, rangeAttribute.Max);

                Vector2 displayValues = ConvertDisplayValue(sliderValues, rangeAttribute, true);
                sliderValues.x = EditorGUI.FloatField(splittedRect[0], displayValues.x);
                sliderValues.y = EditorGUI.FloatField(splittedRect[2], displayValues.y);
                sliderValues = ConvertDisplayValue(sliderValues, rangeAttribute, false);

                if (EditorGUI.EndChangeCheck())
                    property.vector2Value = sliderValues;
            }
            else if (propertyType == SerializedPropertyType.Vector2Int)
            {
                Vector2 sliderValues = property.vector2IntValue;
                EditorGUI.MinMaxSlider(splittedRect[1], ref sliderValues.x, ref sliderValues.y, rangeAttribute.Min, rangeAttribute.Max);

                Vector2 displayValues = ConvertDisplayValue(sliderValues, rangeAttribute, true);
                sliderValues.x = EditorGUI.FloatField(splittedRect[0], (int)displayValues.x);
                sliderValues.y = EditorGUI.FloatField(splittedRect[2], (int)displayValues.y);
                sliderValues = ConvertDisplayValue(sliderValues, rangeAttribute, false);

                Vector2Int propertyOutput = new (Mathf.FloorToInt(sliderValues.x), Mathf.FloorToInt(sliderValues.y));

                if (EditorGUI.EndChangeCheck())
                    property.vector2IntValue = propertyOutput;
            }

            static Vector2 ConvertDisplayValue(Vector2 inputValues, RangeSliderAttribute attributes, bool toDisplay)
            {
                Vector2 inRange, outRange;

                if (toDisplay)
                {
                    inRange = new Vector2(attributes.Min, attributes.Max);
                    outRange = new Vector2(attributes.MinDisplay, attributes.MaxDisplay);
                }
                else
                {
                    inRange = new Vector2(attributes.MinDisplay, attributes.MaxDisplay);
                    outRange = new Vector2(attributes.Min, attributes.Max);
                }

                float clampedMin = Mathf.Clamp(inputValues.x, inRange.x, Mathf.Min(inRange.y, inputValues.y));
                float clampedMax = Mathf.Clamp(inputValues.y, Mathf.Max(clampedMin, inputValues.x), inRange.y);

                float scaledMin = (clampedMin - inRange.x) / (inRange.y - inRange.x) * (outRange.y - outRange.x) + outRange.x;
                float scaledMax = (clampedMax - inRange.x) / (inRange.y - inRange.x) * (outRange.y - outRange.x) + outRange.x;

                return new(scaledMin, scaledMax); ;
            }
        }

        Rect[] SplitRect(Rect rectToSplit, int n)
        {
            Rect[] rects = new Rect[n];

            for (int i = 0; i < n; i++)
            {
                rects[i] = new Rect(
                    rectToSplit.position.x + (i * rectToSplit.width / n),
                    rectToSplit.position.y, rectToSplit.width / n, rectToSplit.height);
            }

            int padding = (int)rects[0].width - 50;
            int space = 5;

            rects[0].width -= padding + space;
            rects[2].width -= padding + space;

            rects[1].x -= padding;
            rects[1].width += padding * 2;

            rects[2].x += padding + space;

            return rects;
        }
    }
}