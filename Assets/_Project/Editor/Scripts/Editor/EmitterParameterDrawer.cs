using UnityEditor;
using UnityEngine;
using PlaneWaver.Emitters;

namespace PlaneWaver.Modulation
{
    [CustomPropertyDrawer(typeof(Parameter))]
    public class EmitterParameterCustomDrawer : PropertyDrawer
    {
        private const int PrefixWidth = 140;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int indent = EditorGUI.indentLevel;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;

            EditorGUIUtility.wideMode = true;
            EditorGUIUtility.labelWidth = PrefixWidth;

            string parameterName = property.FindPropertyRelative("ParameterName").stringValue;
            bool isLength = property.FindPropertyRelative("ParamType").enumValueIndex == (int)ParameterType.Length;
            bool isVolatile = property.serializedObject.targetObject is VolatileEmitterObject;
            Vector2 range = property.FindPropertyRelative("Range").vector2Value;
            
            SerializedProperty inputProperty = property.FindPropertyRelative("Input");
            SerializedProperty outputProperty = property.FindPropertyRelative("Output");
            SerializedProperty noiseProperty = property.FindPropertyRelative("Noise");
            
            label = new GUIContent(parameterName);
            
            EditorGUI.BeginProperty(position, label, property);

            if (isVolatile && !isLength)
                DrawTimeExponent(property);

            DrawNoiseFields(noiseProperty, isVolatile);
            DrawModulationFields(inputProperty, outputProperty, range, isVolatile);

            EditorGUI.indentLevel = indent;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.EndProperty();
        }

        private static void DrawTimeExponent(SerializedProperty property)
        {
            var timeContent = new GUIContent
            ("Time Exponent",
                "Apply exponent to the parameter over the duration of the volatile burst");
            SerializedProperty timeExponent = property.FindPropertyRelative("TimeExponent");
            EditorGUILayout.Slider(timeExponent, 0.1f, 5f, timeContent);
        }

        private static void DrawNoiseFields(SerializedProperty noiseProperty, bool isVolatile)
        {
            SerializedProperty noiseEnabled = noiseProperty.FindPropertyRelative("Enabled");
            SerializedProperty noiseInfluence = noiseProperty.FindPropertyRelative("Amount");
            SerializedProperty noiseMultiplier = noiseProperty.FindPropertyRelative("Factor");

            EditorGUILayout.Space(2);
            var noiseEnabledLabel = new GUIContent
                    ("Noise", "Enable/disable noise modulation of the parameter.");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(noiseEnabledLabel, EditorStyles.toggle, EditorStyles.boldLabel);
                noiseEnabled.boolValue = EditorGUILayout.Toggle(GUIContent.none, noiseEnabled.boolValue);
            }

            if (!noiseEnabled.boolValue)
                return;

            EditorGUI.indentLevel++;
            if (!isVolatile)
            {
                SerializedProperty usePerlin = noiseProperty.FindPropertyRelative("UsePerlin");
                EditorGUILayout.PropertyField(usePerlin);

                if (usePerlin.boolValue)
                {
                    SerializedProperty perlinSpeed = noiseProperty.FindPropertyRelative("PerlinSpeed");
                    EditorGUILayout.Slider
                    (perlinSpeed, 0, 10,
                        new GUIContent
                        ("Perlin Speed",
                            "Speed of the Perlin noise. 0 = no movement, 1 = steady, 10 = extremely fast."));
                }
            }
            else
            {
                SerializedProperty lockNoise = noiseProperty.FindPropertyRelative("VolatileLock");
                EditorGUILayout.PropertyField(lockNoise);
            }

            var noiseMultiplyLabel = new GUIContent
            ("Multiplier",
                "Multiplier for the noise value, providing greater control over the noise influence.");
            EditorGUILayout.Slider(noiseMultiplier, 0, 1, noiseMultiplyLabel);
            var noiseLabel = new GUIContent
            ("Amount",
                "Influence of the noise value on the parameter value. 0 = no influence, 1 = full influence.");
            EditorGUILayout.Slider(noiseInfluence, 0, 1, noiseLabel);
            EditorGUI.indentLevel--;
        }

        private static void DrawModulationFields(
            SerializedProperty input, SerializedProperty output, Vector2 range, bool isVolatile)
        {
            SerializedProperty enabled = input.FindPropertyRelative("Enabled");
            SerializedProperty inputSource = input.FindPropertyRelative("Source");
            SerializedProperty inputGroup = inputSource.FindPropertyRelative("InputGroup");
            SerializedProperty selectedGroup = GetGroup(inputGroup, inputSource);
            SerializedProperty inputRange = input.FindPropertyRelative("Range");
            SerializedProperty absolute = input.FindPropertyRelative("Absolute");
            SerializedProperty inputFactor = input.FindPropertyRelative("Factor");
            SerializedProperty accumulate = input.FindPropertyRelative("Accumulate");
            SerializedProperty exponent = input.FindPropertyRelative("Exponent");
            SerializedProperty smoothing = input.FindPropertyRelative("Smoothing");
            
            SerializedProperty limiter = output.FindPropertyRelative("Limiter");
            SerializedProperty amount = output.FindPropertyRelative("Amount");
            SerializedProperty start = output.FindPropertyRelative("Start");
            SerializedProperty end = output.FindPropertyRelative("End");


            GUILayoutOption[] floatFieldOptions = { GUILayout.MinWidth(40), GUILayout.ExpandWidth(true) };
            bool isInstant = inputSource.FindPropertyRelative("InputGroup").enumValueIndex ==
                             (int)InputGroups.Collision;

            // INPUT
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                var modulationContent = new GUIContent
                        ("Modulation", "Enable/disable modulation of parameter using the selected input");
                EditorGUILayout.PrefixLabel(modulationContent, EditorStyles.toggle, EditorStyles.boldLabel);
                enabled.boolValue = EditorGUILayout.Toggle(GUIContent.none, enabled.boolValue);
            }

            if (!enabled.boolValue)
                return;

            EditorGUI.indentLevel++;
            float paramRange = range.y - range.x;
            float modAmount = amount.floatValue * paramRange;
            var influenceContent = new GUIContent
            ("Influence",
                "Amount of modulation to apply to the parameter. Negative values invert the modulation.");

            EditorGUI.BeginChangeCheck();
            float newAmount = EditorGUILayout.Slider(influenceContent, modAmount, -paramRange, paramRange);

            if (EditorGUI.EndChangeCheck()) { amount.floatValue = newAmount / paramRange; }

            if (isVolatile)
            {
                EditorGUILayout.PrefixLabel("Apply Modulation");
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(start);
                EditorGUILayout.PropertyField(end);
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

                if (EditorGUI.EndChangeCheck()) { selectedGroup = GetGroup(inputGroup, inputSource); }

                EditorGUILayout.PropertyField(selectedGroup, GUIContent.none);
            }

            var rangeContent = new GUIContent("Range", "The range of the input value.");
            EditorGUILayout.PropertyField(inputRange, rangeContent);
            var absoluteContent = new GUIContent
                    ("Absolute", "Use the absolute value from the positive and negative normalised ranged.");
            EditorGUILayout.PropertyField(absolute, absoluteContent);
            var scaleContent = new GUIContent
                    ("Scale", "Scaling factor applied to the input value before normalisation.");
            EditorGUILayout.PropertyField(inputFactor, scaleContent, floatFieldOptions);
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

            if (limiter.enumValueIndex == (int)ModulationLimiter.Clip)
            {
                EditorGUILayout.Slider
                (exponent, 0.1f, 5,
                    new GUIContent
                    ("Exponent",
                        "Exponent for the modulation value. < 1 = log, 1 = linear, > 1 = raised to the power of the exponent."));
            }

            var limitLabel = new GUIContent
            ("Limiting",
                "The mode to use for limiting the parameter value. Clip = clamp to parameter range, Wrap = wrap around the min/max range, PingPong = reverse direction when reaching the min/max range.");
            EditorGUILayout.PropertyField(limiter, limitLabel);
            EditorGUI.indentLevel--;
        }

        private static SerializedProperty GetGroup(SerializedProperty group, SerializedProperty property)
        {
            var input = (InputGroups)group.enumValueIndex;
            return property.FindPropertyRelative(input.ToString());
        }
    }
}