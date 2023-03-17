using System;
using System.Collections.Generic;
using MaxVRAM;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

using MaxVRAM.Extensions;
using PlaneWaver.Library;
using PlaneWaver.Emitters;
using PlaneWaver.Interaction;
using PropertiesObject = PlaneWaver.Modulation.Parameter.PropertiesObject;

namespace PlaneWaver.Modulation
{
    [CustomEditor(typeof(BaseEmitterObject))]
    public class EmitterObjectCustomEditor : Editor
    {
        private int _selectedModIndex;
        private bool _showModulation;
        private bool _volatileTriggered;
        private Parameter.ProcessedValues _processedValues;
        private Parameter _currentParameter;
        
        private AnimBool[] _editSelectionArray;
        private BaseEmitterObject _emitterObject;
        private SerializedProperty _parameterArray;
        private PropertiesObject[] _parameterProperties;
        
        private static float EditorWidth => EditorGUIUtility.currentViewWidth - 30;
        private const int PrefixMinWidth = 40;
        private const int PrefixMaxWidth = 140;
        private const int PrefixWidth = 140;
        private const int SmallIconSize = 18;
        private const int FloatFieldWidth = 40;
        private const int ToolbarIconSize = 48;
        private float _modulationHeaderHalfWidth;
        
        private GUIContent[] _parameterIcons;
        private GUIContent _modulationHeader;

        private GUILayoutOption[] _prefixOptions;
        private GUILayoutOption[] _paramWithIconOptions;
        private GUILayoutOption[] _floatFieldOptions;
        private GUILayoutOption[] _parameterOptions;
        private GUILayoutOption[] _toggleOptions;
        private GUILayoutOption[] _toolbarOptions;

        private GUIStyle _titleStyle;
        private GUIStyle _modulationTypeHeader;
        private GUIStyle _parameterStyle;
        private GUIStyle _toggleStyle;
        
        private ActorObject _actor;
        private bool _actorSet;
        
        public void OnEnable()
        {
            EditorApplication.update += Update;
            _emitterObject = (BaseEmitterObject)target;
            
            _processedValues = new Parameter.ProcessedValues();
            
            _parameterArray = serializedObject.FindProperty("Parameters.Array");
            _parameterProperties = _emitterObject.Parameters.ConvertAll(parameter => parameter.ParameterProperties).ToArray();
            _parameterIcons = new GUIContent[_parameterArray.arraySize];
            
            for (var i = 0; i < _parameterArray.arraySize; i++)
                _parameterIcons[i] = _emitterObject.Parameters[i].GetGUIContent();

            _toggleOptions = new [] { GUILayout.Height(SmallIconSize), GUILayout.Width(SmallIconSize) };
            _prefixOptions = new[] { GUILayout.Width(PrefixWidth) };
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
            
            _titleStyle = new GUIStyle {
                fontSize = 16,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState {
                    textColor = Color.white
                }
            };
            
            _modulationTypeHeader = new GUIStyle {
                fontSize = 12,
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

        private void OnDisable()
        {
            EditorApplication.update += Update;
            if (_actor != null)
                _actor.OnNewValidCollision -= TriggerCollisionEmitters;
            foreach (AnimBool t in _editSelectionArray)
                t.valueChanged.RemoveListener(Repaint);
        }
        
        private void Update()
        {
            if (_actor != null && !_actorSet)
            {
                _actor.OnNewValidCollision += TriggerCollisionEmitters;
            }

            _actorSet = _actor != null;

            if (!_actorSet || !Application.isPlaying || _selectedModIndex < 0)
            {
                _showModulation = false;
                return;
            }
            
            _currentParameter = _emitterObject.Parameters[_selectedModIndex];

            if (_currentParameter == null || !_currentParameter.ModulationData.Enabled)
            {
                _showModulation = false;
                return;
            }
            
            if (_emitterObject is not VolatileEmitterObject || !_processedValues.Instant || _volatileTriggered)
            {
                _volatileTriggered = false;
                _currentParameter.UpdateInputValue(ref _processedValues, _actor);
                Parameter.ProcessModulation(ref _processedValues, in _currentParameter.ModulationData);
            }
            
            _showModulation = true;
            Repaint();
        }
        
        private void TriggerCollisionEmitters(CollisionData data)
        {
            if (_emitterObject is VolatileEmitterObject)
                _volatileTriggered = true;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawGeneralProperties();
            DrawParameters();
            DrawModulationToolbar();
            DrawFadeGroups();
            
            if (_showModulation)
                DrawValuesPreview();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneralProperties()
        {
            const int audioObjectWidth = 64;
        
            SerializedProperty serialisedName = serializedObject.FindProperty("EmitterName");
            SerializedProperty serialisedDescription = serializedObject.FindProperty("Description");
            SerializedProperty serialisedAudioObject = serializedObject.FindProperty("AudioObject");
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Emitter Editor", _titleStyle);
            EditorGUILayout.Space(2);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Emitter Name", _prefixOptions);
                EditorGUILayout.PropertyField(serialisedName, GUIContent.none);
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Description", _prefixOptions);
                EditorGUILayout.PropertyField(serialisedDescription, GUIContent.none);
            }
            
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Audio Object", _prefixOptions);
                EditorGUILayout.PropertyField(serialisedAudioObject, GUIContent.none);
            }
            
            EditorGUILayout.Space();
            
            var bgColor = new GUIStyle {normal = {background = Texture2D.grayTexture}};
            var audioObject = (AudioObject)serialisedAudioObject.objectReferenceValue;

            if (audioObject == null) return;

            Editor audioAssetEditor = CreateEditor(audioObject.Clip);
            audioAssetEditor.OnInteractivePreviewGUI(
                GUILayoutUtility.GetRect(EditorWidth, audioObjectWidth), bgColor);
        }

        private void DrawParameters()
        {
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
                                ? IconManager.ToggleIcons["ModulationOn"]
                                : IconManager.ToggleIcons["ModulationOff"];

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
                            EditorGUILayout.MinMaxSlider
                                    (ref initRange.x, ref initRange.y, paramRangeVector.x, paramRangeVector.y);
                            previousSliderRect = GUILayoutUtility.GetLastRect();
                        }

                        if (_emitterObject.Parameters[i] is not Length)
                        {
                            initRange.y = EditorGUILayout.DelayedFloatField(initRange.y, _floatFieldOptions);
                        }

                        initialRange.vector2Value = initRange;

                        if (_emitterObject is VolatileEmitterObject && _emitterObject.Parameters[i] is not Length)
                        {
                            SerializedProperty reversePath = modulationData.FindPropertyRelative("ReversePath");
                            GUIContent revIcon = reversePath.boolValue
                                    ? IconManager.ToggleIcons["PathReverse"]
                                    : IconManager.ToggleIcons["PathForward"];

                            if (GUILayout.Button(revIcon, _toggleStyle, _toggleOptions))
                                reversePath.boolValue = !reversePath.boolValue;
                        }
                    }

                    SerializedProperty modInfluence = modulationData.FindPropertyRelative("ModInfluence");

                    EditorGUILayout.Space(2);
                    var newFieldRect = new Rect(previousFieldRect) {
                        y = previousFieldRect.y + previousFieldRect.height + 2
                    };
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
        }

        private void DrawModulationToolbar()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Modulation Editor", _titleStyle);
            _actor = (ActorObject)EditorGUILayout.ObjectField(new GUIContent("Test Actor"), _actor, typeof(ActorObject), true);
            EditorGUILayout.Space(3);
            
            Rect toolbarRect;

            var toolbarStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button) {
                stretchWidth = true,
                stretchHeight = true,
                fixedHeight = 0
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                _toolbarOptions = new [] {
                    GUILayout.MaxHeight(ToolbarIconSize),
                    GUILayout.Width(EditorGUIUtility.currentViewWidth - 30)
                };
                
                int prevIndex = _selectedModIndex;
                EditorGUI.BeginChangeCheck();
                int newIndex = UpdateSelectedModulationToggles(
                    GUILayout.Toolbar(_selectedModIndex, _parameterIcons, toolbarStyle, _toolbarOptions));
                toolbarRect = GUILayoutUtility.GetLastRect();

                if (EditorGUI.EndChangeCheck())
                {
                    newIndex = newIndex == prevIndex ? -1 : newIndex;
                    _selectedModIndex = UpdateSelectedModulationToggles(newIndex);

                    if (_selectedModIndex > -1)
                    {
                        bool isInstant = _emitterObject.Parameters[newIndex].ModulationInput.IsInstant;
                        _processedValues = new Parameter.ProcessedValues(isInstant);
                        _modulationHeader = new GUIContent(_parameterProperties[newIndex].Name);

                        if (_actor != null)
                            _emitterObject.Parameters[newIndex].Initialise(_actor);
                    }
                }
            }

            if (_selectedModIndex < 0)
                return;
            
            float toolbarItemWidth = toolbarRect.width / _editSelectionArray.Length;
            float toolbarItemHalf = toolbarItemWidth / 2;
            Rect headerRect = GUILayoutUtility.GetRect(_modulationHeader, _modulationTypeHeader);
            if (headerRect.width > 1) { _modulationHeaderHalfWidth = headerRect.width / 2; }
            var modulationHeaderRect = new Rect(toolbarRect) {
                x = toolbarRect.x + toolbarItemWidth * _selectedModIndex + toolbarItemHalf - _modulationHeaderHalfWidth,
                y = toolbarRect.y + toolbarRect.height + 4
            };
            GUI.Label(modulationHeaderRect, _modulationHeader, _modulationTypeHeader);
        }

        private void DrawFadeGroups()
        {
            // Dynamically display modulation editor for selected parameter
            for (var i = 0; i < _editSelectionArray.Length; i++)
            {
                if (EditorGUILayout.BeginFadeGroup(_editSelectionArray[i].faded))
                    EditorGUILayout.PropertyField(_parameterArray.GetArrayElementAtIndex(i));
                EditorGUILayout.EndFadeGroup();
            }
        }

        private void DrawValuesPreview()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Modulation Preview", _titleStyle);
            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("Input", _processedValues.Input.ToString("F3"));
            EditorGUILayout.Slider(new GUIContent("Normalised"), _processedValues.Normalised, 0, 1);
            EditorGUILayout.LabelField("Scaled", _processedValues.Scaled.ToString("F3"));

            Parameter.ModulationDataObject currentData = _currentParameter.ModulationData;
            
            if (currentData.Accumulate)
                EditorGUILayout.LabelField("Accumulated", _processedValues.Accumulated.ToString("F3"));
            if (currentData.LimiterMode == ModulationLimiter.Clip)
                EditorGUILayout.LabelField("Raised", _processedValues.Raised.ToString("F3"));
            if (!_processedValues.Instant)
                EditorGUILayout.LabelField("Smoothed", _processedValues.Smoothed.ToString("F3"));
            
            
            // TODO - Volatile emitters using CLAMP mode currently do not display correct values in this field or the output field.
            EditorGUILayout.Slider(new GUIContent("Limited"), _processedValues.Limited, 0, 1);
            
            float modValue = _processedValues.Output;

            if (currentData.IsVolatileEmitter)
                modValue += MaxMath.LargestComponent(currentData.InitialRange);
            
            EditorGUILayout.LabelField("Output", modValue.ToString("F3"), new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.Space(3);
        }

        private int UpdateSelectedModulationToggles(int index)
        {
            if (index == _selectedModIndex) return index;
            for (var i = 0; i < _editSelectionArray.Length; i++)
                _editSelectionArray[i].target = i == index;
            return index;
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