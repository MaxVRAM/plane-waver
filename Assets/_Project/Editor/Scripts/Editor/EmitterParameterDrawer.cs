using UnityEditor;
using UnityEngine;

using MaxVRAM.CustomGUI;
using PlaneWaver.Emitters;

namespace PlaneWaver.Modulation
{
    [CustomPropertyDrawer(typeof(Parameter))]
    public class EmitterParameterCustomDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int indent = EditorGUI.indentLevel;
            EditorGUIUtility.wideMode = true;
            EditorGUI.indentLevel = 0;
            
            SerializedProperty parameterProperties = property.FindPropertyRelative("ParameterProperties");
            SerializedProperty modulationInput = property.FindPropertyRelative("ModulationInput");
            SerializedProperty modulationData = property.FindPropertyRelative("ModulationData");
            SerializedProperty enabled = modulationData.FindPropertyRelative("Enabled");
            SerializedProperty inputGroup = modulationInput.FindPropertyRelative("InputGroup");
            SerializedProperty selectedGroup = GetGroup(inputGroup, modulationInput);
            SerializedProperty modInputRange = modulationData.FindPropertyRelative("ModInputRange");
            SerializedProperty modInputMultiplier = modulationData.FindPropertyRelative("ModInputMultiplier");
            SerializedProperty accumulate = modulationData.FindPropertyRelative("Accumulate");
            SerializedProperty smoothing = modulationData.FindPropertyRelative("Smoothing");
            SerializedProperty modExponent = modulationData.FindPropertyRelative("ModExponent");
            SerializedProperty modInfluence = modulationData.FindPropertyRelative("ModInfluence");
            SerializedProperty limiterMode = modulationData.FindPropertyRelative("LimiterMode");
            SerializedProperty noiseInfluence = modulationData.FindPropertyRelative("NoiseInfluence");
            SerializedProperty noiseMultiplier = modulationData.FindPropertyRelative("NoiseMultiplier");
            
            Vector2 paramRangeVector = parameterProperties.FindPropertyRelative("ParameterRange").vector2Value;
            string parameterName = parameterProperties.FindPropertyRelative("Name").stringValue;
            bool isVolatile = (BaseEmitterObject)property.serializedObject.targetObject is VolatileEmitterObject;
            
            GUILayoutOption[] floatFieldOptions = { GUILayout.MinWidth(40), GUILayout.ExpandWidth(true) };
            
            GUILayoutOption[] labelWidthOption = {
                GUILayout.Width(EditorGUIUtility.labelWidth),
                GUILayout.ExpandWidth(false)
            };
            
            label = new GUIContent(parameterName);
            EditorGUI.BeginProperty(position, label, property);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Modulate " + parameterName, EditorStyles.boldLabel, labelWidthOption);
                EditorGUILayout.Toggle(GUIContent.none, enabled.boolValue);

                float paramRange = paramRangeVector.y - paramRangeVector.x;
                float modAmount = modInfluence.floatValue * paramRange;
                
                // TODO - fix the alignment of the slider
                if (enabled.boolValue)
                {
                    float newAmount = EditorGUILayout.Slider(GUIContent.none, modAmount, -paramRange, paramRange);
                    if (Mathf.Approximately(modAmount, newAmount)) { modInfluence.floatValue = modAmount / paramRange; }
                }
            }
            
            if (!enabled.boolValue)
            {
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
                return;
            }

            EditorGUILayout.Space();
            
            // INPUT
            using (new EditorGUILayout.HorizontalScope())
            {
                // TODO - maybe don't pair the header with the drop menus. Try dropping it down one.
                GUILayout.Label("Input Source", EditorStyles.boldLabel, labelWidthOption);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(inputGroup, GUIContent.none);
                if (EditorGUI.EndChangeCheck()) { selectedGroup = GetGroup(inputGroup, modulationInput); }
                EditorGUILayout.PropertyField(selectedGroup, GUIContent.none);
            }
            
            EditorGUI.indentLevel++;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("    Input Range", labelWidthOption);
                float inputMin = modInputRange.vector2Value.x;
                float inputMax = modInputRange.vector2Value.y;
                var minContent = new GUIContent("Min", "Value normalised to 0 from the input source; lowest value accepted by the parameter.");
                var maxContent = new GUIContent("Max", "Value normalised to 1 from the input source; highest value accepted by the parameter.");
                GUILayout.Label(minContent);
                inputMin = EditorGUILayout.FloatField(GUIContent.none, inputMin, floatFieldOptions);
                GUILayout.Label(maxContent);
                inputMax = EditorGUILayout.FloatField(GUIContent.none, inputMax, floatFieldOptions);
                modInputRange.vector2Value = new Vector2(inputMin, inputMax);
            }
            var scaleContent = new GUIContent("Scale", "Scaling factor applied to the input value after normalisation.");
            EditorGUILayout.PropertyField(modInputMultiplier, scaleContent, floatFieldOptions);
            EditorGUI.indentLevel--;
            
            // PROCESSING
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Processing", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(accumulate, new GUIContent(
                "Accumulate", "If true, the modulation value will accumulate over time, otherwise the instantaneous input value will be used."));
            EditorGUILayout.Slider(
                smoothing, 0, 1, new GUIContent(
                    "Smoothing", "Smoothing factor for the modulation value. 0 = no smoothing, 1 = full smoothing."));
            EditorGUILayout.Slider( modExponent, 0.1f, 5, new GUIContent(
                "Modulation Exponent", "Exponent for the modulation value. < 1 = log, 1 = linear, > 1 = raised to the power of the exponent."));
            EditorGUI.indentLevel--;
            
            // LIMITING
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                var limitLabel = new GUIContent
                ("Limiting",
                    "The mode to use for limiting the parameter value. Clip = clamp to parameter range, Wrap = wrap around the min/max range, PingPong = reverse direction when reaching the min/max range.");
                GUILayout.Label(limitLabel, EditorStyles.boldLabel, labelWidthOption);
                EditorGUILayout.PropertyField(limiterMode, GUIContent.none);
            }
            
            if (isVolatile)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    SerializedProperty fixedStart = parameterProperties.FindPropertyRelative("FixedStart");
                    SerializedProperty fixedEnd = parameterProperties.FindPropertyRelative("FixedEnd");
                    GUILayout.Label("    Ignore Modulation", labelWidthOption);
                    fixedStart.boolValue = GUILayout.Toggle(fixedStart.boolValue, "Start");
                    fixedEnd.boolValue = GUILayout.Toggle(fixedEnd.boolValue, "End");
                }
            }
            
            // NOISE
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                // TODO - maybe don't pair the header with the slider. Try dropping the slider down one.
                var noiseLabel = new GUIContent("Noise", "Influence of the noise value on the parameter value. 0 = no influence, 1 = full influence.");
                GUILayout.Label(noiseLabel, EditorStyles.boldLabel, labelWidthOption);
                EditorGUILayout.Slider(noiseInfluence, 0, 1, GUIContent.none);
            }
            EditorGUI.indentLevel++;
            var noiseMultiplyLabel = new GUIContent("Multiplier", "Multiplier for the noise value, providing greater control over the noise influence.");
            EditorGUILayout.Slider(noiseMultiplier, 0, 1, noiseMultiplyLabel);

            if (!isVolatile)
            {
                SerializedProperty usePerlin = modulationData.FindPropertyRelative("UsePerlin");
                // TODO - perform value change check here
                usePerlin.boolValue = EditorGUILayout.PropertyField(usePerlin);
                
                if (usePerlin.boolValue)
                {
                    SerializedProperty perlinSpeed = modulationData.FindPropertyRelative("PerlinSpeed");
                    EditorGUILayout.Slider(perlinSpeed, 0, 10, new GUIContent(
                        "Perlin Speed", "Speed of the Perlin noise. 0 = no movement, 1 = steady, 10 = extremely fast."));
                }
            }
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
        
        private static SerializedProperty GetGroup(SerializedProperty group, SerializedProperty property)
        {
            var input = (InputGroups) group.enumValueIndex;
            return property.FindPropertyRelative(input.ToString());
        }  
    }
}