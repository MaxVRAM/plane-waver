using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.AnimatedValues;

using MaxVRAM.Extensions;
using PlaneWaver.Emitters;

namespace PlaneWaver.Modulation
{
    public class AssetHandler
    {
        [OnOpenAsset]
        public static bool OpenEditor(int instanceId, int line)
        {
            switch (EditorUtility.InstanceIDToObject(instanceId))
            {
                case StableEmitterObject emitter:
                    EmitterObjectEditorWindow.Open(emitter);
                    return true;
                case VolatileEmitterObject emitter:
                    EmitterObjectEditorWindow.Open(emitter);
                    return true;
                default:
                    return false;
            }
        }
    }

    [CustomEditor(typeof(BaseEmitterObject))]
    public class EmitterObjectCustomEditor : Editor
    {
        private bool _isVolatile;
        private int _selectedParameterIndex;
        private AnimBool[] _parameterToggles;

        private SerializedProperty _emitterName;
        private SerializedProperty _description;
        private SerializedProperty _audioAsset;
        private SerializedProperty _parameterArray;
        private string[] _parameterNames;
        private bool[] _enabledParameters;

        // Unity Docs GUIStyle EditorStyles reference
        // Ref: https://docs.unity3d.com/2021.3/Documentation/ScriptReference/EditorStyles.html
        private GUIStyle _titleStyle;
        
        private const int PrefixWidth = 30;
        private const int ToggleWidth = 30;
        private const int FloatWidth = 40;
        private GUILayoutOption _prefixWidthOption;
        private GUILayoutOption _toggleLabelWidth;
        private GUILayoutOption _floatFieldWidth;
        

        // private EmitterObject _emitterObject;
        // private Parameter[] _parameterObjects;
        // private SerializedProperty[] _parameterProperties;

        public void OnEnable()
        {
            _isVolatile = (BaseEmitterObject)target is VolatileEmitterObject;
            _emitterName = serializedObject.FindProperty("EmitterName");
            _description = serializedObject.FindProperty("Description");
            _audioAsset = serializedObject.FindProperty("AudioObject");
            _parameterArray = serializedObject.FindProperty("Parameters.Array");
            _parameterNames = GetParameterNameArray(_parameterArray);
            _enabledParameters = new bool[_parameterArray.arraySize];
            
            _titleStyle = new GUIStyle {
                fontSize = 16,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState {
                    textColor = Color.white
                },
                margin = new RectOffset(0, 0, 4, 2)
            };
            
            
            _prefixWidthOption = GUILayout.Width(PrefixWidth);
            _toggleLabelWidth = GUILayout.Width(ToggleWidth);
            _floatFieldWidth = GUILayout.Width(FloatWidth);
            
            _selectedParameterIndex = -1;
            _parameterToggles = new AnimBool[_parameterArray.arraySize];

            for (var i = 0; i < _parameterToggles.Length; i++)
            {
                _parameterToggles[i] = new AnimBool(false);
                _parameterToggles[i].valueChanged.AddListener(Repaint);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(_emitterName);
            EditorGUILayout.PropertyField(_description);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_audioAsset);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Parameters", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Mod", EditorStyles.boldLabel, _toggleLabelWidth);
            if (_isVolatile) { EditorGUILayout.LabelField("Inv", EditorStyles.boldLabel, _toggleLabelWidth); }
            EditorGUILayout.LabelField(_isVolatile ? "Lifetime Path" : "Initial Range", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            for (var i = 0; i < _parameterArray.arraySize; i++)
            {
                SerializedProperty currentParam = _parameterArray.GetArrayElementAtIndex(i);
                SerializedProperty parameterProperties = currentParam.FindPropertyRelative("ParameterProperties");
                SerializedProperty modulationInput = currentParam.FindPropertyRelative("ModulationInput");
                SerializedProperty modulationData = currentParam.FindPropertyRelative("ModulationData");
                SerializedProperty modulationEnabled = modulationData.FindPropertyRelative("ModulationEnabled");
                SerializedProperty initialRange = modulationData.FindPropertyRelative("InitialRange");
                SerializedProperty parameterRange = modulationData.FindPropertyRelative("ParameterRange");
                
                // Could use GUIStyleState.background to determine the direction of the modulation
                // Ref: https://docs.unity3d.com/2021.3/Documentation/ScriptReference/GUIStyleState-background.html
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(GetParameterName(currentParam));
                
                if (_isVolatile)
                {
                    SerializedProperty invertVolatileRange = modulationData.FindPropertyRelative("InvertVolatileRange");
                    invertVolatileRange.boolValue = EditorGUILayout.Toggle(invertVolatileRange.boolValue, _toggleLabelWidth);
                }
                
                modulationEnabled.boolValue = EditorGUILayout.Toggle(modulationEnabled.boolValue, _toggleLabelWidth);
                _enabledParameters[i] = modulationEnabled.boolValue;
                
                Vector2 range = initialRange.vector2Value;
                range.x = range.x.RoundDigits(4);
                range.y = range.y.RoundDigits(4);
                range.x = EditorGUILayout.DelayedFloatField(range.x, _floatFieldWidth);
                EditorGUILayout.MinMaxSlider
                        (ref range.x, ref range.y, parameterRange.vector2Value.x, parameterRange.vector2Value.y);
                range.y = EditorGUILayout.DelayedFloatField(range.y, _floatFieldWidth);
                initialRange.vector2Value = range;
                
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Modulation Editor", _titleStyle);
            ChangeDisplayedParameter(GUILayout.SelectionGrid(_selectedParameterIndex, _parameterNames, 3));
            EditorGUILayout.Space();

            for (var i = 0; i < _parameterToggles.Length; i++)
            {
                if (EditorGUILayout.BeginFadeGroup(_parameterToggles[i].faded))
                {
                    EditorGUI.indentLevel++;
                    SerializedProperty parameter = _parameterArray.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(parameter);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }

        private string GetParameterName(SerializedProperty parameter)
        {
            return parameter.FindPropertyRelative("ParameterProperties").FindPropertyRelative("Name").stringValue;
        }

        private string[] GetParameterNameArray(SerializedProperty parameterArray)
        {
            var parameterNames = new string[parameterArray.arraySize];
            for (var i = 0; i < parameterNames.Length; i++)
                parameterNames[i] = _parameterArray.GetArrayElementAtIndex(i).FindPropertyRelative
                        ("ParameterProperties").FindPropertyRelative("Name").stringValue;
            return parameterNames;
        }

        private void ChangeDisplayedParameter(int index)
        {
            if (index == _selectedParameterIndex) return;

            _selectedParameterIndex = index;
            for (var i = 0; i < _parameterToggles.Length; i++)
                _parameterToggles[i].target = i == index;
        }

        private void OnDisable()
        {
            foreach (AnimBool t in _parameterToggles)
                t.valueChanged.RemoveListener(Repaint);
        }
    }

    [CustomEditor(typeof(StableEmitterObject))]
    public class StableEmitterObjectCustomEditor : EmitterObjectCustomEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // var stableEmitter = (StableEmitterObject)target;
            //
            // if (GUILayout.Button("Open Editor"))
            //     EmitterObjectEditorWindow.Open(stableEmitter);            
        }
    }

    [CustomEditor(typeof(VolatileEmitterObject))]
    public class VolatileEmitterObjectCustomEditor : EmitterObjectCustomEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // var volatileEmitter = (VolatileEmitterObject)target;
            //
            // if (GUILayout.Button("Open Editor"))
            //     EmitterObjectEditorWindow.Open(volatileEmitter);
        }
    }
}