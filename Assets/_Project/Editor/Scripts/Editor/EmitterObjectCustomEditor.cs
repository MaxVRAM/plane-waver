using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

using MaxVRAM.Extensions;
using PlaneWaver.Emitters;
using PlaneWaver.Library;
using PropertiesObject = PlaneWaver.Modulation.Parameter.PropertiesObject;

namespace PlaneWaver.Modulation
{
    [CustomEditor(typeof(BaseEmitterObject))]
    public class EmitterObjectCustomEditor : Editor
    {
        private bool _isVolatile;
        private int _selectedModIndex;
        private AnimBool[] _editSelectionArray;
        private BaseEmitterObject _emitterObject;
        private SerializedProperty _emitterName;
        private SerializedProperty _description;
        private SerializedProperty _audioObject;
        private SerializedProperty _parameterArray;
        private PropertiesObject[] _parameterProperties;
        
        private static float EditorWidth => EditorGUIUtility.currentViewWidth - 30;
        private const int PrefixMinWidth = 40;
        private const int PrefixMaxWidth = 120;
        private const int PrefixWidth = 120;
        private const int SmallIconSize = 18;
        private const int FloatFieldWidth = 40;
        private const int ToolbarIconSize = 48;
        private const int AudioObjectWidth = 64;
        
        private Dictionary<string, GUIContent> _toggleIcons;
        private GUIContent[] _parameterIcons;

        private GUILayoutOption[] _prefixOptions;
        private GUILayoutOption[] _paramPrefixOptions;
        private GUILayoutOption[] _floatFieldOptions;
        private GUILayoutOption[] _parameterOptions;
        private GUILayoutOption[] _toggleOptions;
        private GUILayoutOption[] _toolbarOptions;
        
        private GUIStyle _titleStyle;
        private GUIStyle _parameterStyle;
        private GUIStyle _toggleStyle;
        private GUIStyle _toolbarStyle;
        
        public void OnEnable()
        {
            _emitterObject = (BaseEmitterObject)target;
            _isVolatile = _emitterObject is VolatileEmitterObject;
            _emitterName = serializedObject.FindProperty("EmitterName");
            _description = serializedObject.FindProperty("Description");
            _audioObject = serializedObject.FindProperty("AudioObject");
            _parameterArray = serializedObject.FindProperty("Parameters.Array");
            _parameterProperties = _emitterObject.Parameters.ConvertAll(parameter => parameter.ParameterProperties).ToArray();
            _parameterIcons = new GUIContent[_parameterArray.arraySize];
            
            for (var i = 0; i < _parameterArray.arraySize; i++)
                _parameterIcons[i] = _emitterObject.Parameters[i].GetGUIContent();

            _toggleIcons = new Dictionary<string, GUIContent>
            {
                {"ModulationOn", new GUIContent(IconManager.GetIcon("ModulationOn"), "Modulation On")},
                {"ModulationOff", new GUIContent(IconManager.GetIcon("ModulationOff"), "Modulation Off")},
                {"PathForward", new GUIContent(IconManager.GetIcon("PathForward"), "Path Forward. " +
                    "Parameter value will traverse the range in a FORWARD direction over the duration of the grain burst.")},
                {"PathReverse", new GUIContent(IconManager.GetIcon("PathReverse"), "Path Reverse. " +
                    "Parameter value will traverse the range in a REVERSE direction over the duration of the grain burst.")}
            };

            _toggleOptions = new [] { GUILayout.Height(SmallIconSize), GUILayout.Width(SmallIconSize) };
            _prefixOptions = new[] { GUILayout.Width(PrefixWidth) };
            _paramPrefixOptions = new[] { GUILayout.Width(PrefixWidth - SmallIconSize) };
            _floatFieldOptions = new[] { GUILayout.Width(FloatFieldWidth) };
            
            _toggleStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button) {
                margin = new RectOffset(0,0,2,0),
                padding = new RectOffset(1,1,1,1),
                border = new RectOffset(1,1,1,1),
                normal = {
                    background = Texture2D.grayTexture
                },
                active = {
                    background = Texture2D.whiteTexture
                }
            };

            _toolbarStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button) {
                stretchWidth = true,
                stretchHeight = true,
                fixedHeight = 0
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
            _editSelectionArray = new AnimBool[_parameterArray.arraySize];

            for (var i = 0; i < _editSelectionArray.Length; i++)
            {
                _editSelectionArray[i] = new AnimBool(false);
                _editSelectionArray[i].valueChanged.AddListener(Repaint);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Emitter Editor", _titleStyle);
            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Emitter Name", _prefixOptions);
                EditorGUILayout.PropertyField(_emitterName, GUIContent.none);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Description", _prefixOptions);
                EditorGUILayout.PropertyField(_description, GUIContent.none);
            }
            
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Audio Object", _prefixOptions);
                EditorGUILayout.PropertyField(_audioObject, GUIContent.none);
            }
            
            EditorGUILayout.Space();
            
            var bgColor = new GUIStyle {normal = {background = Texture2D.grayTexture}};
            var audioObject = (AudioObject)_audioObject.objectReferenceValue;

            if (audioObject != null)
            {
                Editor audioAssetEditor = CreateEditor(audioObject.Clip);
                audioAssetEditor.OnInteractivePreviewGUI(
                    GUILayoutUtility.GetRect(EditorWidth, AudioObjectWidth), bgColor);
            }

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
                        EditorGUILayout.LabelField(_parameterIcons[i], _toggleOptions);
                        EditorGUILayout.LabelField(_parameterProperties[i].Name, _paramPrefixOptions);
                        
                        SerializedProperty modulationEnabled = modulationData.FindPropertyRelative("ModulationEnabled");
                        GUIContent modIcon = modulationEnabled.boolValue
                                ? _toggleIcons["ModulationOn"]
                                : _toggleIcons["ModulationOff"];

                        
                        if (GUILayout.Button(modIcon, _toggleStyle, _toggleOptions))
                            modulationEnabled.boolValue = !modulationEnabled.boolValue;
                        
                        Vector2 initRange = initialRange.vector2Value;
                        initRange.x = initRange.x.RoundDigits(4);
                        initRange.y = initRange.y.RoundDigits(4);
                        initRange.x = EditorGUILayout.DelayedFloatField(initRange.x, _floatFieldOptions);

                        if (_emitterObject.Parameters[i] is Length)
                        {
                            initRange.x = GUILayout.HorizontalSlider( initRange.x,
                                _parameterProperties[i].ParameterRange.x,
                                _parameterProperties[i].ParameterRange.y);
                        }
                        else
                            EditorGUILayout.MinMaxSlider(
                                ref initRange.x, ref initRange.y,
                                _parameterProperties[i].ParameterRange.x,
                                _parameterProperties[i].ParameterRange.y);

                        if (_emitterObject.Parameters[i] is not Length)
                        {
                            initRange.y = EditorGUILayout.DelayedFloatField(initRange.y, _floatFieldOptions);

                            if (_isVolatile)
                            {
                                SerializedProperty reversePath = modulationData.FindPropertyRelative("ReversePath");
                                GUIContent revIcon = reversePath.boolValue
                                        ? _toggleIcons["PathReverse"]
                                        : _toggleIcons["PathForward"];

                                if (GUILayout.Button(revIcon, _toggleStyle, _toggleOptions))
                                    reversePath.boolValue = !reversePath.boolValue;
                            }
                        }
                        
                        initialRange.vector2Value = initRange;
                    }
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Modulation Editor", _titleStyle);
            EditorGUILayout.Space(2);

            // Toolbar for selecting parameter modulation
            using (new EditorGUILayout.HorizontalScope())
            {
                _toolbarOptions = new [] {
                    GUILayout.MaxHeight(ToolbarIconSize),
                    GUILayout.Width(EditorGUIUtility.currentViewWidth - 30)
                };
                
                EditorGUI.BeginChangeCheck();
                int prevIndex = _selectedModIndex;
                int newIndex = UpdateSelectedModulationToggles(
                    GUILayout.Toolbar(_selectedModIndex, _parameterIcons, _toolbarStyle, _toolbarOptions));
                if (EditorGUI.EndChangeCheck()) newIndex = newIndex == prevIndex ? -1 : newIndex;
                _selectedModIndex = UpdateSelectedModulationToggles(newIndex);
            }
            
            EditorGUI.indentLevel++;
            // Dynamically display modulation editor for selected parameter
            for (var i = 0; i < _editSelectionArray.Length; i++)
            {
                if (EditorGUILayout.BeginFadeGroup(_editSelectionArray[i].faded))
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
            for (var i = 0; i < _editSelectionArray.Length; i++)
                _editSelectionArray[i].target = i == index;
            return index;
        }

        private void OnDisable()
        {
            foreach (AnimBool t in _editSelectionArray)
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