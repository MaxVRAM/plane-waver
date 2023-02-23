using UnityEditor;
using UnityEngine;

using MaxVRAM;

[CustomPropertyDrawer(typeof(ValueSliderAttribute))]
public class ValueSlider : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ValueSliderAttribute valueAttribute = (ValueSliderAttribute)attribute;
        SerializedPropertyType propertyType = property.propertyType;

        label.tooltip = valueAttribute.Min.ToString("F2") + " to " + valueAttribute.Max.ToString("F2");
        Rect controlRect = EditorGUI.PrefixLabel(position, label);
        Rect[] splittedRect = SplitRect(controlRect);

        EditorGUI.BeginChangeCheck();

        if (propertyType == SerializedPropertyType.Float)
        {
            float sliderValue = property.floatValue;
            EditorGUI.Slider(splittedRect[1], sliderValue, valueAttribute.Min, valueAttribute.Max);

            float displayValue = ConvertDisplayValue(sliderValue, valueAttribute, true);
            sliderValue = EditorGUI.FloatField(splittedRect[0], displayValue);
            sliderValue = ConvertDisplayValue(sliderValue, valueAttribute, false);

            if (EditorGUI.EndChangeCheck())
                property.floatValue = sliderValue;
        }
        else if (propertyType == SerializedPropertyType.Integer)
        {
            int sliderValue = property.intValue;
            EditorGUI.Slider(splittedRect[1], sliderValue, valueAttribute.Min, valueAttribute.Max);

            int displayValue = (int)ConvertDisplayValue(sliderValue, valueAttribute, true);
            sliderValue = EditorGUI.IntField(splittedRect[0], displayValue);
            sliderValue = (int)ConvertDisplayValue(sliderValue, valueAttribute, false);

            if (EditorGUI.EndChangeCheck())
                property.intValue = sliderValue;
        }

        static float ConvertDisplayValue(float inputValue, ValueSliderAttribute attributes, bool toDisplay)
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

            float outValue = Mathf.Clamp(inputValue, inRange.x, inRange.y);
            return MaxMath.Map(outValue, inRange.x, inRange.y, outRange.x, outRange.y);
        }
    }

    Rect[] SplitRect(Rect rectToSplit)
    {
        Rect[] rects = new Rect[2];

        for (int i = 0; i < 2; i++)
        {
            rects[i] = new Rect(
                rectToSplit.position.x + (i * rectToSplit.width / 2),
                rectToSplit.position.y, rectToSplit.width / 2, rectToSplit.height);
        }

        int padding = (int)rects[0].width - 50;
        int space = 5;

        rects[0].width -= padding + space;

        rects[1].x -= padding;
        rects[1].width += padding * 1;

        return rects;
    }
}