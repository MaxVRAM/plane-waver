using System;
using System.Collections.Generic;
using System.Globalization;
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
        private bool _isVolatileEmitter;
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
        
        public ActorObject Actor;
        private bool _actorSet;
        
        public void OnEnable()
        {
            EditorApplication.update += Update;
            _emitterObject = (BaseEmitterObject)target;
            _isVolatileEmitter = _emitterObject is VolatileEmitterObject;
            _processedValues = new Parameter.ProcessedValues();
            _parameterProperties = _emitterObject.Parameters.ConvertAll(parameter => parameter.ParameterProperties).ToArray();
            
            _parameterArray = serializedObject.FindProperty("Parameters.Array");
            _parameterIcons = new GUIContent[_parameterArray.arraySize];
            
            for (var i = 0; i < _parameterArray.arraySize; i++)
                _parameterIcons[i] = _emitterObject.Parameters[i].GetGUIContent();

            _toggleOptions = new [] { GUILayout.Height(SmallIconSize), GUILayout.Width(SmallIconSize) };
            _prefixOptions = new[] { GUILayout.Width(PrefixWidth) };
            _paramWithIconOptions = new[] { GUILayout.Width(PrefixWidth - SmallIconSize * 2) };
            _floatFieldOptions = new[] { GUILayout.Width(FloatFieldWidth) };
            
            DefineStyles();
            
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
            if (Actor != null)
                Actor.OnNewValidCollision -= TriggerCollisionEmitters;
            foreach (AnimBool t in _editSelectionArray)
                t.valueChanged.RemoveListener(Repaint);
        }
        
        private void Update()
        {
            if (Actor != null && !_actorSet)
            {
                Actor.OnNewValidCollision += TriggerCollisionEmitters;
            }

            _actorSet = Actor != null;

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
            
            if (!_isVolatileEmitter || !_processedValues.Instant || _volatileTriggered)
            {
                _volatileTriggered = false;
                _currentParameter.UpdateInputValue(ref _processedValues, Actor);
                _currentParameter.ModulationData.IsVolatileEmitter = _isVolatileEmitter;
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
            EditorGUI.BeginChangeCheck();
            var newActor = (ActorObject)EditorGUILayout.ObjectField(new GUIContent("Test Actor"), Actor, typeof(ActorObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                Actor = newActor;
                ReinitialisePreviewObjects();
            }
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
                    ReinitialisePreviewObjects();
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

        private void ReinitialisePreviewObjects()
        {
            if (_selectedModIndex <= -1) return;

            bool isInstant = _emitterObject.Parameters[_selectedModIndex].ModulationInput.IsInstant;
            _processedValues = new Parameter.ProcessedValues(isInstant);
            _modulationHeader = new GUIContent(_parameterProperties[_selectedModIndex].Name);
            _emitterObject.Parameters[_selectedModIndex].IsVolatileEmitter = _isVolatileEmitter;

            if (Actor != null)
                _emitterObject.Parameters[_selectedModIndex].Initialise(Actor);
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

            EditorGUILayout.LabelField("Input", _processedValues.Input.RoundDigits(4).ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.Slider(new GUIContent("Normalised"), _processedValues.Normalised, 0, 1);
            EditorGUILayout.LabelField("Scaled", _processedValues.Scaled.RoundDigits(4).ToString(CultureInfo.InvariantCulture));

            Parameter.ModulationDataObject currentData = _currentParameter.ModulationData;
            
            if (currentData.Accumulate)
                EditorGUILayout.LabelField("Accumulated", _processedValues.Accumulated.RoundDigits(4).ToString(CultureInfo.InvariantCulture));
            if (currentData.LimiterMode == ModulationLimiter.Clip)
                EditorGUILayout.LabelField("Raised", _processedValues.Raised.RoundDigits(4).ToString(CultureInfo.InvariantCulture));
            if (!_processedValues.Instant)
                EditorGUILayout.LabelField("Smoothed", _processedValues.Smoothed.RoundDigits(4).ToString(CultureInfo.InvariantCulture));

            float paramRange = _currentParameter.ParameterProperties.ParameterRange.y - _currentParameter.ParameterProperties.ParameterRange.x;
            float initialOffset = currentData.InitialValue;
            float modValue = _processedValues.Output;

            if (_isVolatileEmitter)
            {
                initialOffset = currentData.ReversePath ? currentData.InitialRange.y : currentData.InitialRange.x;
                EditorGUILayout.Slider(new GUIContent("Limited"), _processedValues.Limited, 0, 1);
                modValue = Mathf.Clamp(modValue += initialOffset, -paramRange, paramRange);
            }

            EditorGUILayout.LabelField("Initial Offset", initialOffset.RoundDigits(4).ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Output", modValue.RoundDigits(4).ToString(CultureInfo.InvariantCulture), new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.Space(3);
        }

        private int UpdateSelectedModulationToggles(int index)
        {
            if (index == _selectedModIndex) return index;
            for (var i = 0; i < _editSelectionArray.Length; i++)
                _editSelectionArray[i].target = i == index;
            return index;
        }

        private void DefineStyles()
        {
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