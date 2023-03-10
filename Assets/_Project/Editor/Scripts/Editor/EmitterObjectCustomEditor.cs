using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.AnimatedValues;

using MaxVRAM.Extensions;
using PlaneWaver.Emitters;
using PropertiesObject = PlaneWaver.Modulation.Parameter.PropertiesObject;

namespace PlaneWaver.Modulation
{
    public class AssetHandler
    {
        [OnOpenAsset]
        public static bool OpenEditor(int instanceId, int line)
        {
            switch (EditorUtility.InstanceIDToObject(instanceId))
            {
                case StableEmitter emitter:
                    EmitterObjectEditorWindow.Open(emitter);
                    return true;
                case VolatileEmitter emitter:
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
        private int _selectedModIndex;
        private AnimBool[] _parameterToggles;
        private BaseEmitterObject _emitterObject;
        private SerializedProperty _emitterName;
        private SerializedProperty _description;
        private SerializedProperty _audioAsset;
        private SerializedProperty _parameterArray;
        private PropertiesObject[] _parameterProperties;

        // Unity Docs GUIStyle EditorStyles reference
        // Ref: https://docs.unity3d.com/2021.3/Documentation/ScriptReference/EditorStyles.html
        private static GUIStyle _titleStyle;
        private static GUIStyle _toolbarStyle;
        
        private const int PrefixWidth = 30;
        private const int ToggleWidth = 30;
        private const int FloatWidth = 40;
        
        private GUILayoutOption _prefixWidthOption;
        private GUILayoutOption _toggleLabelWidth;
        private GUILayoutOption _floatFieldWidth;
        
        private const int ToggleSize = 24;
        private const int ParameterSize = 64;
        
        private GUIContent[] _parameterIcons;
        private Dictionary<string, GUIContent> _modulationIcons;
        private GUILayoutOption[] _toggleOptions;
        private GUILayoutOption[] _parameterOptions;
        private GUILayoutOption[] _modulationOptions;
        private GUIStyle _toggleStyle;
        private GUIStyle _parameterStyle;
        
        public void OnEnable()
        {
            _emitterObject = (BaseEmitterObject)target;
            _isVolatile = _emitterObject is VolatileEmitter;
            _emitterName = serializedObject.FindProperty("EmitterName");
            _description = serializedObject.FindProperty("Description");
            _audioAsset = serializedObject.FindProperty("AudioObject");
            _parameterArray = serializedObject.FindProperty("Parameters.Array");
            _parameterProperties = _emitterObject.Parameters.ConvertAll(parameter => parameter.ParameterProperties).ToArray();
            _parameterIcons = new GUIContent[_parameterArray.arraySize];
            
            for (var i = 0; i < _parameterArray.arraySize; i++)
                _parameterIcons[i] = _emitterObject.Parameters[i].GetGUIContent();

            _modulationIcons = new Dictionary<string, GUIContent>
            {
                {"ModulationOn", new GUIContent(IconManager.GetIcon("ModulationOn"), "Modulation On")},
                {"ModulationOff", new GUIContent(IconManager.GetIcon("ModulationOff"), "Modulation Off")},
                {"PathForward", new GUIContent(IconManager.GetIcon("PathForward"), "Path Forward. " +
                    "Parameter value will traverse the range in a FORWARD direction over the duration of the grain burst.")},
                {"PathReverse", new GUIContent(IconManager.GetIcon("PathReverse"), "Path Reverse. " +
                    "Parameter value will traverse the range in a REVERSE direction over the duration of the grain burst.")}
            };

            _prefixWidthOption = GUILayout.Width(PrefixWidth);
            _toggleLabelWidth = GUILayout.Width(ToggleWidth);
            _floatFieldWidth = GUILayout.Width(FloatWidth);
            
            _toggleOptions = new [] {
                GUILayout.Height(ToggleSize),
                GUILayout.Width(ToggleSize)
            };
            
            _toggleStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button) {
                // fixedWidth = ToggleSize,
                // fixedHeight = ToggleSize,
                margin = new RectOffset(0,0,0,0),
                padding = new RectOffset(0,0,0,0)
            };
            
            _parameterStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button) {
                // fixedHeight = ParameterSize,
                // stretchWidth = true,
                // stretchHeight = false
            };
            
            _titleStyle = new GUIStyle {
                fontSize = 16,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState {
                    textColor = Color.white
                }
            };
            
            _selectedModIndex = -1;
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
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Emitter Editor", _titleStyle);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.PropertyField(_emitterName);
            EditorGUILayout.PropertyField(_description);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_audioAsset);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Parameters", _titleStyle);
            EditorGUILayout.Space(2);

            // Parameter list with basic configuration
            using (new EditorGUILayout.VerticalScope())
            {
                for (var i = 0; i < _parameterArray.arraySize; i++)
                {
                    SerializedProperty currentParam = _parameterArray.GetArrayElementAtIndex(i);
                    SerializedProperty modulationData = currentParam.FindPropertyRelative("ModulationData");
                    SerializedProperty initialRange = modulationData.FindPropertyRelative("InitialRange");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel(_parameterProperties[i].Name);

                        SerializedProperty modulationEnabled = modulationData.FindPropertyRelative("ModulationEnabled");
                        GUIContent modIcon = modulationEnabled.boolValue
                                ? _modulationIcons["ModulationOn"]
                                : _modulationIcons["ModulationOff"];

                        if (GUILayout.Button(modIcon, _toggleStyle, _toggleOptions))
                            modulationEnabled.boolValue = !modulationEnabled.boolValue;

                        Vector2 initRange = initialRange.vector2Value;
                        initRange.x = initRange.x.RoundDigits(4);
                        initRange.y = initRange.y.RoundDigits(4);
                        initRange.x = EditorGUILayout.DelayedFloatField(initRange.x, _floatFieldWidth);

                        EditorGUILayout.MinMaxSlider
                        (ref initRange.x, ref initRange.y, _parameterProperties[i].ParameterRange.x,
                            _parameterProperties[i].ParameterRange.y);

                        initRange.y = EditorGUILayout.DelayedFloatField(initRange.y, _floatFieldWidth);
                        initialRange.vector2Value = initRange;

                        if (!_isVolatile) continue;

                        SerializedProperty reversePath = modulationData.FindPropertyRelative("ReversePath");
                        GUIContent revIcon = reversePath.boolValue
                                ? _modulationIcons["PathReverse"]
                                : _modulationIcons["PathForward"];

                        if (GUILayout.Button(revIcon, _toggleOptions))
                            reversePath.boolValue = !reversePath.boolValue;
                    }
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Modulation Editor", _titleStyle);
            EditorGUILayout.Space(2);
            
            //
            // _parameterOptions = new [] {
            //     GUILayout.MaxHeight(ParameterSize),
            //     GUILayout.Width(EditorGUIUtility.currentViewWidth)
            // };

            // Toolbar for selecting parameter modulation
            using (new EditorGUILayout.HorizontalScope())
            {
                _parameterOptions = new [] {
                    GUILayout.MaxHeight(ParameterSize),
                    //GUILayout.Width(Screen.width - 20)
                    GUILayout.Width(EditorGUIUtility.currentViewWidth - 20)
                };
                
                EditorGUI.BeginChangeCheck();
                int prevIndex = _selectedModIndex;
                int newIndex = UpdateSelectedModulationToggles(
                    GUILayout.Toolbar(_selectedModIndex, _parameterIcons, _parameterStyle, _parameterOptions));
                if (EditorGUI.EndChangeCheck()) newIndex = newIndex == prevIndex ? -1 : newIndex;
                _selectedModIndex = UpdateSelectedModulationToggles(newIndex);
            }
            
            EditorGUI.indentLevel++;
            // Dynamically display modulation editor for selected parameter
            for (var i = 0; i < _parameterToggles.Length; i++)
            {
                if (EditorGUILayout.BeginFadeGroup(_parameterToggles[i].faded))
                    EditorGUILayout.PropertyField(_parameterArray.GetArrayElementAtIndex(i));
                EditorGUILayout.EndFadeGroup();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }

        private int UpdateSelectedModulationToggles(int index)
        {
            if (index == _selectedModIndex) return index;
            for (var i = 0; i < _parameterToggles.Length; i++)
                _parameterToggles[i].target = i == index;
            return index;
        }

        private void OnDisable()
        {
            foreach (AnimBool t in _parameterToggles)
                t.valueChanged.RemoveListener(Repaint);
        }
    }

    [CustomEditor(typeof(StableEmitter))]
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

    [CustomEditor(typeof(VolatileEmitter))]
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