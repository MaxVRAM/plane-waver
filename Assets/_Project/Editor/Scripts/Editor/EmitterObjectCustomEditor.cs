using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

using MaxVRAM.Extensions;
using PlaneWaver.Library;
using PlaneWaver.Emitters;
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
        private const int PrefixWidth = 140;
        private const int SmallIconSize = 18;
        private const int FloatFieldWidth = 40;
        private const int ToolbarIconSize = 48;
        private const int AudioObjectWidth = 64;
        private float modulationHeaderHalfWidth;
        
        private Dictionary<string, GUIContent> _toggleIcons;
        private GUIContent[] _parameterIcons;
        private GUIContent _modulationHeader;

        private GUILayoutOption[] _prefixOptions;
        private GUILayoutOption[] _paramNoIconOptions;
        private GUILayoutOption[] _paramWithIconOptions;
        private GUILayoutOption[] _floatFieldOptions;
        private GUILayoutOption[] _parameterOptions;
        private GUILayoutOption[] _toggleOptions;
        private GUILayoutOption[] _toolbarOptions;

        private GUIStyle _titleStyle;
        private GUIStyle _modulationTypeHeader;
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
            _paramNoIconOptions = new[] { GUILayout.Width(PrefixWidth - SmallIconSize) };
            _paramWithIconOptions = new[] { GUILayout.Width(PrefixWidth - SmallIconSize * 2) };
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
            
            _modulationTypeHeader = new GUIStyle {
                fontSize = 14,
                stretchWidth = false,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                normal = new GUIStyleState {
                    textColor = Color.white
                }
            };
            
            _modulationHeader = GUIContent.none;
            
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

            Rect previousFieldRect;
            Rect previousSliderRect;
            
            // Parameter list with basic configuration
            using (new EditorGUILayout.VerticalScope())
            {
                for (var i = 0; i < _parameterArray.arraySize; i++)
                {
                    SerializedProperty currentParam = _parameterArray.GetArrayElementAtIndex(i);
                    SerializedProperty modulationData = currentParam.FindPropertyRelative("ModulationData");
                    SerializedProperty initialRange = modulationData.FindPropertyRelative("InitialRange");
                    SerializedProperty modulationEnabled = modulationData.FindPropertyRelative("Enabled");
                 
                    Vector2 paramRangeVector = _parameterProperties[i].ParameterRange;
                    float paramRange = paramRangeVector.y - paramRangeVector.x;
                    
                    EditorGUILayout.BeginVertical();
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(_parameterIcons[i], _toggleOptions);
                        EditorGUILayout.LabelField(_parameterProperties[i].Name, _paramWithIconOptions);
                    
                        GUIContent modIcon = modulationEnabled.boolValue
                                ? _toggleIcons["ModulationOn"]
                                : _toggleIcons["ModulationOff"];

                        if (GUILayout.Button(modIcon, _toggleStyle, _toggleOptions))
                            modulationEnabled.boolValue = !modulationEnabled.boolValue;
                        
                        Vector2 initRange = initialRange.vector2Value;
                        initRange.x = initRange.x.RoundDigits(4);
                        initRange.y = initRange.y.RoundDigits(4);
                        initRange.x = EditorGUILayout.DelayedFloatField(initRange.x, _floatFieldOptions);
                        previousFieldRect = GUILayoutUtility.GetLastRect();

                        if (_emitterObject.Parameters[i] is Length)
                        {
                            initRange.x = GUILayout.HorizontalSlider(initRange.x, paramRangeVector.x, paramRangeVector.y);
                            previousSliderRect = GUILayoutUtility.GetLastRect();
                        }
                        else
                        {
                            EditorGUILayout.MinMaxSlider(ref initRange.x, ref initRange.y, paramRangeVector.x, paramRangeVector.y);
                            previousSliderRect = GUILayoutUtility.GetLastRect();
                        }
                        
                        if (_emitterObject.Parameters[i] is not Length)
                        {
                            initRange.y = EditorGUILayout.DelayedFloatField(initRange.y, _floatFieldOptions);
                        }

                        initialRange.vector2Value = initRange;
                        
                        if (_isVolatile && _emitterObject.Parameters[i] is not Length)
                        {
                                SerializedProperty reversePath = modulationData.FindPropertyRelative("ReversePath");
                                GUIContent revIcon = reversePath.boolValue
                                        ? _toggleIcons["PathReverse"]
                                        : _toggleIcons["PathForward"];

                                if (GUILayout.Button(revIcon, _toggleStyle, _toggleOptions))
                                    reversePath.boolValue = !reversePath.boolValue;
                        }
                    }
                    
                    SerializedProperty modInfluence = modulationData.FindPropertyRelative("ModInfluence");
                    
                    EditorGUILayout.Space(2);
                    var newFieldRect = new Rect(previousFieldRect) { y = previousFieldRect.y + previousFieldRect.height + 2 };
                    float modAmount = modInfluence.floatValue * paramRange;
                    modAmount = modAmount.RoundDigits(3);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(GUIContent.none, _paramWithIconOptions);
                        using (new EditorGUI.DisabledGroupScope(!modulationEnabled.boolValue))
                        {
                            modAmount = EditorGUI.DelayedFloatField(newFieldRect, modAmount);
                            Rect newSliderRect = previousSliderRect;
                            newSliderRect.y = newFieldRect.y;
                            modAmount = GUI.HorizontalSlider(newSliderRect, modAmount, -paramRange, paramRange);

                            if (!Mathf.Approximately(modInfluence.floatValue, modAmount))
                            {
                                modAmount = Mathf.Clamp(modAmount, -paramRange, paramRange);
                                modInfluence.floatValue = modAmount / paramRange;
                            }
                        }
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Modulation Editor", _titleStyle);
            EditorGUILayout.Space(2);

            
            // Toolbar for selecting parameter modulation
            Rect toolbarRect;

            using (new EditorGUILayout.HorizontalScope())
            {
                _toolbarOptions = new [] {
                    GUILayout.MaxHeight(ToolbarIconSize),
                    GUILayout.Width(EditorGUIUtility.currentViewWidth - 30)
                };
                
                int prevIndex = _selectedModIndex;
                EditorGUI.BeginChangeCheck();
                int newIndex = UpdateSelectedModulationToggles(
                    GUILayout.Toolbar(_selectedModIndex, _parameterIcons, _toolbarStyle, _toolbarOptions));
                toolbarRect = GUILayoutUtility.GetLastRect();

                if (EditorGUI.EndChangeCheck())
                {
                    newIndex = newIndex == prevIndex ? -1 : newIndex;
                    if (newIndex > -1)
                        _modulationHeader = new GUIContent(_parameterProperties[newIndex].Name);
                }
                _selectedModIndex = UpdateSelectedModulationToggles(newIndex);
            }

            if (_selectedModIndex > -1)
            {
                float toolbarItemWidth = toolbarRect.width / _editSelectionArray.Length;
                float toolbarItemHalf = toolbarItemWidth / 2;
                Rect headerRect = GUILayoutUtility.GetRect(_modulationHeader, _modulationTypeHeader);
                if (headerRect.width > 1) { modulationHeaderHalfWidth = headerRect.width / 2; }
                var modulationHeaderRect = new Rect(toolbarRect) {
                    x = toolbarRect.x + toolbarItemWidth * _selectedModIndex + toolbarItemHalf - modulationHeaderHalfWidth,
                    y = toolbarRect.y + toolbarRect.height + 4
                };
                GUI.Label(modulationHeaderRect, _modulationHeader, _modulationTypeHeader);
            }
            
            // Dynamically display modulation editor for selected parameter
            for (var i = 0; i < _editSelectionArray.Length; i++)
            {
                if (EditorGUILayout.BeginFadeGroup(_editSelectionArray[i].faded))
                {
                    EditorGUILayout.PropertyField(_parameterArray.GetArrayElementAtIndex(i));
                }
                EditorGUILayout.EndFadeGroup();
            }
            
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
        
        void BlankAction(Rect position)
        {
        
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