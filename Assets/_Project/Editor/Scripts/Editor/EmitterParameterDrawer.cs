using MaxVRAM.Extensions;
using UnityEditor;
using UnityEngine;

using PlaneWaver.Emitters;

namespace PlaneWaver.Modulation
{
    [CustomPropertyDrawer(typeof(Parameter))]
    public class EmitterParameterCustomDrawer : PropertyDrawer
    {
        private bool isVolatile;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int indent = EditorGUI.indentLevel;
            
            isVolatile = (BaseEmitterObject)property.serializedObject.targetObject is VolatileEmitterObject;
            SerializedProperty parameterProperties = property.FindPropertyRelative("ParameterProperties");
            SerializedProperty modulationInput = property.FindPropertyRelative("ModulationInput");
            SerializedProperty modulationData = property.FindPropertyRelative("ModulationData");
            SerializedProperty enabled = modulationData.FindPropertyRelative("Enabled");
            SerializedProperty modInputRange = modulationData.FindPropertyRelative("ModInputRange");
            SerializedProperty modInputMultiplier = modulationData.FindPropertyRelative("ModInputMultiplier");
            SerializedProperty accumulate = modulationData.FindPropertyRelative("Accumulate");
            SerializedProperty smoothing = modulationData.FindPropertyRelative("Smoothing");
            SerializedProperty modExponent = modulationData.FindPropertyRelative("ModExponent");
            SerializedProperty modInfluence = modulationData.FindPropertyRelative("ModInfluence");
            SerializedProperty limiterMode = modulationData.FindPropertyRelative("LimiterMode");
            SerializedProperty noiseInfluence = modulationData.FindPropertyRelative("NoiseInfluence");
            SerializedProperty noiseMultiplier = modulationData.FindPropertyRelative("NoiseMultiplier");
            
            string parameterName = parameterProperties.FindPropertyRelative("Name").stringValue;
            label = new GUIContent(parameterName);
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.PropertyField(enabled, new GUIContent(
                "Modulate " + parameterName, "If true, the parameter will be affected by the modulation value."));
            
            if (!enabled.boolValue)
            {
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
                return;
            }
                    
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(modulationInput, new GUIContent(
                "Input Source", "The modulation input source value to use for this parameter."));
            
            if (isVolatile)
            {
                SerializedProperty fixedStart = parameterProperties.FindPropertyRelative("FixedStart");
                SerializedProperty fixedEnd = parameterProperties.FindPropertyRelative("FixedEnd");
                EditorGUILayout.PropertyField(fixedStart, new GUIContent(
                    "Fixed Start", "If true, the start value of the parameter will be locked, otherwise will be affected by the modulation value."));
                EditorGUILayout.PropertyField(fixedEnd, new GUIContent(
                    "Fixed End", "If true, the end value of the parameter will be locked, otherwise will be affected by the modulation value."));
            }
            
            EditorGUILayout.PropertyField(modInputRange);
            EditorGUILayout.PropertyField(modInputMultiplier);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Processing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(accumulate, new GUIContent(
                "Accumulate", "If true, the modulation value will accumulate over time, otherwise the instantaneous input value will be used."));

            EditorGUILayout.Slider(
                smoothing, 0, 1, new GUIContent(
                    "Smoothing", "Smoothing factor for the modulation value. 0 = no smoothing, 1 = full smoothing."));

            EditorGUILayout.Slider( modExponent, 0.1f, 5, new GUIContent(
                "Modulation Exponent", "Exponent for the modulation value. < 1 = log, 1 = linear, > 1 = raised to the power of the exponent."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUILayout.Slider(modInfluence, -1, 1, new GUIContent(
                "Modulation Influence", "Influence of the modulation value on the parameter value. 0 = no influence, 1 = full influence, -1 = full inverse influence."));
            
            EditorGUILayout.PropertyField(limiterMode, new GUIContent(
                "Limiter Mode", "The mode to use for limiting the parameter value. Clip = clamp to parameter range, Wrap = wrap around the min/max range, PingPong = reverse direction when reaching the min/max range."));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Additional Noise", EditorStyles.boldLabel);
            
            EditorGUILayout.Slider (noiseInfluence, 0, 1, new GUIContent(
                "Noise Influence", "Influence of the noise value on the parameter value. 0 = no influence, 1 = full influence."));
            
            EditorGUILayout.Slider(noiseMultiplier, 0, 1, new GUIContent(
                "Noise Multiplier", "Multiplier for the noise value, providing greater control over the noise influence."));

            if (!isVolatile)
            {
                SerializedProperty usePerlin = modulationData.FindPropertyRelative("UsePerlin");
                SerializedProperty perlinSpeed = modulationData.FindPropertyRelative("PerlinSpeed");
                EditorGUILayout.PropertyField(usePerlin);
                EditorGUILayout.Slider (perlinSpeed, 0, 10, new GUIContent(
                    "Perlin Speed", "Speed of the Perlin noise. 0 = no movement, 1 = steady, 10 = extremely fast."));
            }
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}