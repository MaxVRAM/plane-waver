using UnityEditor;
using UnityEngine;

using PlaneWaver.Emitters;
using PlaneWaver.Interaction;

namespace PlaneWaver.Modulation
{
    [CustomPropertyDrawer(typeof(Parameter))]
    public class EmitterParameterCustomDrawer : PropertyDrawer
    {
        private const int ToggleWidth = 20;
        private const int PrefixWidth = 140;
        private ActorObject _actor;
        private bool _actorSet;

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
            float labelWidth = EditorGUIUtility.labelWidth;
            
            label = new GUIContent(parameterName);
            EditorGUI.BeginProperty(position, label, property);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Influence", EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                enabled.boolValue = EditorGUILayout.Toggle(GUIContent.none, enabled.boolValue, EditorStyles.toggle, GUILayout.Width(ToggleWidth));

                if (enabled.boolValue)
                {
                    float paramRange = paramRangeVector.y - paramRangeVector.x;
                    float modAmount = modInfluence.floatValue * paramRange;
                    EditorGUI.BeginChangeCheck();
                    float newAmount = EditorGUILayout.Slider(GUIContent.none, modAmount, -paramRange, paramRange);
                    if (EditorGUI.EndChangeCheck()) { modInfluence.floatValue = newAmount / paramRange; }
                }
            }
            
            if (!enabled.boolValue)
            {
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
                return;
            }
            
            if (isVolatile)
            {
                SerializedProperty fixedStart = parameterProperties.FindPropertyRelative("FixedStart");
                SerializedProperty fixedEnd = parameterProperties.FindPropertyRelative("FixedEnd");
                EditorGUI.indentLevel++;
                EditorGUILayout.PrefixLabel("Ignore Modulation");
                EditorGUI.indentLevel++;
                fixedStart.boolValue = EditorGUILayout.ToggleLeft("At Start", fixedStart.boolValue);
                fixedEnd.boolValue = EditorGUILayout.ToggleLeft("At End", fixedEnd.boolValue);
                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(3);
            EditorGUILayout.ObjectField(new GUIContent("Test Actor"), _actor, typeof(ActorObject), true);
            _actorSet = _actor != null;
            EditorGUILayout.Space(3);

            // INPUT
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            using (new EditorGUILayout.HorizontalScope())
            {
                var sourceContent = new GUIContent("Source", "The source of the modulation input." );
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(inputGroup, sourceContent);
                if (EditorGUI.EndChangeCheck()) { selectedGroup = GetGroup(inputGroup, modulationInput); }
                EditorGUILayout.PropertyField(selectedGroup, GUIContent.none);
            }
            
            var scaleContent = new GUIContent("Scale", "Scaling factor applied to the input value before normalisation.");
            EditorGUILayout.PropertyField(modInputMultiplier, scaleContent, floatFieldOptions);
            EditorGUILayout.Vector2Field(new GUIContent("Range", "The range of the input value."), modInputRange.vector2Value);
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
                "Exponent", "Exponent for the modulation value. < 1 = log, 1 = linear, > 1 = raised to the power of the exponent."));

            var limitLabel = new GUIContent("Limiting",
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
                    EditorGUILayout.Slider(perlinSpeed, 0, 10, new GUIContent(
                        "Perlin Speed", "Speed of the Perlin noise. 0 = no movement, 1 = steady, 10 = extremely fast."));
                }
            }
            
            var noiseMultiplyLabel = new GUIContent("Multiplier", "Multiplier for the noise value, providing greater control over the noise influence.");
            EditorGUILayout.Slider(noiseMultiplier, 0, 1, noiseMultiplyLabel);
            var noiseLabel = new GUIContent("Amount", "Influence of the noise value on the parameter value. 0 = no influence, 1 = full influence.");
            EditorGUILayout.Slider(noiseInfluence, 0, 1, noiseLabel);
            
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