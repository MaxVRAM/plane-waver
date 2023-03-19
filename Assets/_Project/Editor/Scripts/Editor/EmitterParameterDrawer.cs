using UnityEditor;
using UnityEngine;
using PlaneWaver.Emitters;

namespace PlaneWaver.Modulation
{
    [CustomPropertyDrawer(typeof(Parameter))]
    public class EmitterParameterCustomDrawer : PropertyDrawer
    {
        private const int ToggleWidth = 20;
        private const int PrefixWidth = 140;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int indent = EditorGUI.indentLevel;
            EditorGUIUtility.wideMode = true;
            EditorGUI.indentLevel = 0;

            GUILayoutOption[] floatFieldOptions = { GUILayout.MinWidth(40), GUILayout.ExpandWidth(true) };
            EditorGUIUtility.labelWidth = PrefixWidth;

            SerializedProperty parameterProperties = property.FindPropertyRelative("ParameterProperties");
            SerializedProperty modulationInput = property.FindPropertyRelative("ModulationInput");
            SerializedProperty modulationData = property.FindPropertyRelative("ModulationData");
            SerializedProperty enabled = modulationData.FindPropertyRelative("Enabled");
            SerializedProperty inputGroup = modulationInput.FindPropertyRelative("InputGroup");
            SerializedProperty selectedGroup = GetGroup(inputGroup, modulationInput);
            SerializedProperty modInputRange = modulationData.FindPropertyRelative("ModInputRange");
            SerializedProperty modInputAbsolute = modulationData.FindPropertyRelative("Absolute");
            SerializedProperty modInputMultiplier = modulationData.FindPropertyRelative("ModInputMultiplier");
            SerializedProperty accumulate = modulationData.FindPropertyRelative("Accumulate");
            SerializedProperty smoothing = modulationData.FindPropertyRelative("Smoothing");
            SerializedProperty inputExponent = modulationData.FindPropertyRelative("InputExponent");
            SerializedProperty modInfluence = modulationData.FindPropertyRelative("ModInfluence");
            SerializedProperty limiterMode = modulationData.FindPropertyRelative("LimiterMode");
            SerializedProperty noiseInfluence = modulationData.FindPropertyRelative("NoiseInfluence");
            SerializedProperty noiseMultiplier = modulationData.FindPropertyRelative("NoiseMultiplier");

            Vector2 paramRangeVector = parameterProperties.FindPropertyRelative("ParameterRange").vector2Value;
            string parameterName = parameterProperties.FindPropertyRelative("Name").stringValue;
            bool isVolatile = (BaseEmitterObject)property.serializedObject.targetObject is VolatileEmitterObject;
            bool isInstant = modulationInput.FindPropertyRelative("InputGroup").enumValueIndex == (int)InputGroups.Collision;
            bool isLength = parameterName == "Burst Length";
            float labelWidth = EditorGUIUtility.labelWidth;

            label = new GUIContent(parameterName);
            EditorGUI.BeginProperty(position, label, property);
            
            // INPUT
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope() )
            {
                var modulationContent = new GUIContent(
                    "Modulation", "Enable/disable modulation of " + parameterName + " using the selected input");
                EditorGUILayout.PrefixLabel(modulationContent, EditorStyles.toggle, EditorStyles.boldLabel);
                enabled.boolValue = EditorGUILayout.Toggle(GUIContent.none, enabled.boolValue);
            }

            if (!enabled.boolValue)
            {
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
                return;
            }
            
            EditorGUI.indentLevel++;
            float paramRange = paramRangeVector.y - paramRangeVector.x;
            float modAmount = modInfluence.floatValue * paramRange;
            var influenceContent = new GUIContent
                    ("Influence", "Amount of modulation to apply to the parameter. Negative values invert the modulation.");

            EditorGUI.BeginChangeCheck();
            float newAmount = EditorGUILayout.Slider(influenceContent, modAmount, -paramRange, paramRange);
            if (EditorGUI.EndChangeCheck()) { modInfluence.floatValue = newAmount / paramRange; }

            if (isVolatile)
            {
                var timeContent = new GUIContent
                        ("Time Exponent", "Apply exponent to the parameter over the duration of the volatile burst");
                SerializedProperty timeExponent = modulationData.FindPropertyRelative("TimeExponent");
                SerializedProperty fixedStart = parameterProperties.FindPropertyRelative("FixedStart");
                SerializedProperty fixedEnd = parameterProperties.FindPropertyRelative("FixedEnd");
                if (!isLength)
                    EditorGUILayout.Slider(timeExponent, 0.1f, 5f, timeContent);
                EditorGUILayout.PrefixLabel("Ignore Modulation");
                EditorGUI.indentLevel++;
                fixedStart.boolValue = EditorGUILayout.ToggleLeft("At Start", fixedStart.boolValue);
                fixedEnd.boolValue = EditorGUILayout.ToggleLeft("At End", fixedEnd.boolValue);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;

            // INPUT
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            using (new EditorGUILayout.HorizontalScope())
            {
                var sourceContent = new GUIContent("Source", "The source of the modulation input.");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(inputGroup, sourceContent);

                if (EditorGUI.EndChangeCheck()) { selectedGroup = GetGroup(inputGroup, modulationInput); }

                EditorGUILayout.PropertyField(selectedGroup, GUIContent.none);
            }

            var rangeContent = new GUIContent("Range", "The range of the input value.");
            EditorGUILayout.PropertyField(modInputRange, rangeContent);
            var absoluteContent = new GUIContent("Absolute", "Use the absolute value from the positive and negative normalised ranged.");
            EditorGUILayout.PropertyField(modInputAbsolute, absoluteContent);
            var scaleContent = new GUIContent("Scale", "Scaling factor applied to the input value before normalisation.");
            EditorGUILayout.PropertyField(modInputMultiplier, scaleContent, floatFieldOptions);
            EditorGUI.indentLevel--;

            // PROCESSING
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Processing", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField
            (accumulate,
                new GUIContent
                ("Accumulate",
                    "If true, the modulation value will accumulate over time, otherwise the instantaneous input value will be used."));
            if (!isInstant)
                EditorGUILayout.Slider
                (smoothing, 0, 1,
                    new GUIContent
                    ("Smoothing",
                        "Smoothing factor for the modulation value. 0 = no smoothing, 1 = full smoothing."));

            if (limiterMode.enumValueIndex == (int)ModulationLimiter.Clip)
            {
                EditorGUILayout.Slider
                (inputExponent, 0.1f, 5,
                    new GUIContent
                    ("Exponent",
                        "Exponent for the modulation value. < 1 = log, 1 = linear, > 1 = raised to the power of the exponent."));
            }

            var limitLabel = new GUIContent
            ("Limiting",
                "The mode to use for limiting the parameter value. Clip = clamp to parameter range, Wrap = wrap around the min/max range, PingPong = reverse direction when reaching the min/max range.");
            EditorGUILayout.PropertyField(limiterMode, limitLabel);

            EditorGUI.indentLevel--;

            // NOISE
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Noise", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (!isVolatile)
            {
                SerializedProperty usePerlin = modulationData.FindPropertyRelative("UsePerlin");
                EditorGUILayout.PropertyField(usePerlin);

                if (usePerlin.boolValue)
                {
                    SerializedProperty perlinSpeed = modulationData.FindPropertyRelative("PerlinSpeed");
                    EditorGUILayout.Slider
                    (perlinSpeed, 0, 10,
                        new GUIContent
                        ("Perlin Speed",
                            "Speed of the Perlin noise. 0 = no movement, 1 = steady, 10 = extremely fast."));
                }
            }

            var noiseMultiplyLabel = new GUIContent
                    ("Multiplier", "Multiplier for the noise value, providing greater control over the noise influence.");
            EditorGUILayout.Slider(noiseMultiplier, 0, 1, noiseMultiplyLabel);
            var noiseLabel = new GUIContent
                    ("Amount", "Influence of the noise value on the parameter value. 0 = no influence, 1 = full influence.");
            EditorGUILayout.Slider(noiseInfluence, 0, 1, noiseLabel);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static SerializedProperty GetGroup(SerializedProperty group, SerializedProperty property)
        {
            var input = (InputGroups)group.enumValueIndex;
            return property.FindPropertyRelative(input.ToString());
        }
    }
}