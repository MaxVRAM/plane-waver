using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MaxVRAM;
using MaxVRAM.CustomGUI;
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
        private bool _isPaused;
        private bool _showModulation;
        private bool _isVolatileEmitter;
        private bool _volatileTriggered;
        private Parameter.ProcessedValues[] _processedValues;
        private Parameter _currentParameter;
        
        private AnimBool[] _editSelectionArray;
        private BaseEmitterObject _emitterObject;
        private SerializedProperty _parameterArray;
        private PropertiesObject[] _parameterProperties;
        private float[] _modulationPreviewValues;
        
        private static float EditorWidth => EditorGUIUtility.currentViewWidth - 30;
        private const int PrefixMinWidth = 40;
        private const int PrefixMaxWidth = 140;
        private const int PrefixWidth = 140;
        private const int SmallIconSize = 18;
        private const int FloatFieldWidth = 40;
        private const int ToolbarIconSize = 48;
        private float _modulationHeaderHalfWidth;
        private float _parameterSliderWidth;
        
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
        private GUIStyle _modInputPreviewStyle;
        private GUIStyle _modValuePreviewStyle;

        private Rect _previousLabelRect;
        private Rect _prevFieldRect;
        private Rect _previousSliderRect;

        private Color _colourDarkGrey;
        
        public ActorObject Actor;
        private bool _actorSet;
        
        
        /// <summary>
        /// Define GUI styles and options for the Emitter Object when the inspector is enabled.
        /// </summary>
        public void OnEnable()
        {
            EditorApplication.update += Update;
            EditorApplication.pauseStateChanged += HandlePauseState;
            _emitterObject = (BaseEmitterObject)target;
            _isVolatileEmitter = _emitterObject is VolatileEmitterObject;
            _parameterProperties = _emitterObject.Parameters.ConvertAll(parameter => parameter.ParameterProperties).ToArray();
            
            _parameterArray = serializedObject.FindProperty("Parameters.Array");
            
            _modulationPreviewValues = new float[_parameterArray.arraySize];
            
            _processedValues = new Parameter.ProcessedValues[_parameterArray.arraySize];
            
            for (var i = 0; i < _parameterArray.arraySize; i++)
                _processedValues[i] = new Parameter.ProcessedValues();
            
            _parameterIcons = new GUIContent[_parameterArray.arraySize];
            
            for (var i = 0; i < _parameterArray.arraySize; i++)
                _parameterIcons[i] = _emitterObject.Parameters[i].GetGUIContent();

            DefineStyles();
            
            _selectedModIndex = -1;
            _editSelectionArray = new AnimBool[_parameterArray.arraySize];

            for (var i = 0; i < _editSelectionArray.Length; i++)
            {
                _editSelectionArray[i] = new AnimBool(false);
                _editSelectionArray[i].valueChanged.AddListener(Repaint);
            }
        }

        /// <summary>
        /// Unity event handler to remove event listeners when this GUI editor is inactive.
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.update -= Update;
            EditorApplication.pauseStateChanged -= HandlePauseState;
            if (Actor != null)
                Actor.OnNewValidCollision -= TriggerCollisionEmitters;
            foreach (AnimBool t in _editSelectionArray)
                t.valueChanged.RemoveListener(Repaint);
        }
        
        /// <summary>
        /// Event handler for when the editor is paused.
        /// </summary>
        /// <param name="state">Boolean value representing the paused state of the active editor.</param>
        private void HandlePauseState(PauseState state)
        {
            _isPaused = state == PauseState.Paused;
            
            if (_isPaused)
                EditorApplication.update -= Update;
            else
                EditorApplication.update += Update;
        }
        
        /// <summary>
        /// Uses Update delegate to update the modulation preview values when running in play mode with an actor attached.
        /// </summary>
        private void Update()
        {
            if (Actor != null && !_actorSet)
            {
                Actor.OnNewValidCollision += TriggerCollisionEmitters;
            }

            _actorSet = Actor != null;

            if (!_actorSet || !Application.isPlaying || _isPaused)
                return;
            
            for (var i = 0; i < _emitterObject.Parameters.Count; i++)
            {
                _emitterObject.Parameters[i].ModulationData.IsVolatileEmitter = _isVolatileEmitter;
                _processedValues[i].Instant = _emitterObject.Parameters[i].ModulationInput.IsInstant;
                
                if (_isVolatileEmitter && _processedValues[i].Instant && !_volatileTriggered)
                    continue;
                
                _emitterObject.Parameters[i].UpdateInputValue(ref _processedValues[i], Actor);
                Parameter.ProcessModulation(ref _processedValues[i], in _emitterObject.Parameters[i].ModulationData);
                _modulationPreviewValues[i] = _processedValues[i].Output;
            }

            if (_selectedModIndex >= 0 && _selectedModIndex < _emitterObject.Parameters.Count)
                _currentParameter = _emitterObject.Parameters[_selectedModIndex];
            else
                _currentParameter = null;
            
            _showModulation = _currentParameter != null && _currentParameter.ModulationData.Enabled;
            _volatileTriggered = false;
            Repaint();
        }
        
        /// <summary>
        /// Handler for OnNewValidCollision event of the actor object to trigger collision values for volatile emitters.
        /// </summary>
        /// <param name="data"></param>
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
            DrawModulationActorHeader();
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
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(GUIContent.none);
            MaxGUI.EditorUILine(_colourDarkGrey);
            EditorGUILayout.Space(2);
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
                    SerializedProperty modulationEnabled = modulationData.FindPropertyRelative("Enabled");

                    Vector2 paramRangeVector = _parameterProperties[i].ParameterRange;
                    float paramRange = paramRangeVector.y - paramRangeVector.x;

                    EditorGUILayout.BeginVertical();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(_parameterIcons[i], _toggleOptions);
                        EditorGUILayout.LabelField(_parameterProperties[i].Name, _paramWithIconOptions);
                        _previousLabelRect = GUILayoutUtility.GetLastRect();

                        GUIContent modIcon = modulationEnabled.boolValue
                                ? IconManager.ToggleIcons["ModulationOn"]
                                : IconManager.ToggleIcons["ModulationOff"];

                        if (GUILayout.Button(modIcon, _toggleStyle, _toggleOptions))
                            modulationEnabled.boolValue = !modulationEnabled.boolValue;
                        
                        Vector2 initRange = initialRange.vector2Value;
                        initRange.x = initRange.x.RoundDigits(4);
                        initRange.y = initRange.y.RoundDigits(4);
                        initRange.x = EditorGUILayout.DelayedFloatField(initRange.x, _floatFieldOptions);
                        _prevFieldRect = GUILayoutUtility.GetLastRect();

                        if (_emitterObject.Parameters[i] is not Length)
                        {
                            EditorGUILayout.MinMaxSlider(
                                ref initRange.x,
                                ref initRange.y,
                                paramRangeVector.x,
                                paramRangeVector.y);
                            _previousSliderRect = GUILayoutUtility.GetLastRect();
                            if (_previousSliderRect.width > 1)
                                _parameterSliderWidth = _previousSliderRect.width;
                        }
                        else
                        {
                            initRange.x = GUILayout.HorizontalSlider(
                                initRange.x,
                                paramRangeVector.x,
                                paramRangeVector.y, GUILayout.Width(_parameterSliderWidth));
                            _previousSliderRect = GUILayoutUtility.GetLastRect();
                        }

                        if (_emitterObject.Parameters[i] is not Length)
                            initRange.y = EditorGUILayout.DelayedFloatField(initRange.y, _floatFieldOptions);

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

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string inputPreviewLabel = modulationEnabled.boolValue
                                ? _emitterObject.Parameters[i].ModulationInput.GetInputName()
                                : " - ";
                        
                        EditorGUILayout.LabelField(GUIContent.none, _paramWithIconOptions);
                        Rect previewLabelRect = GUILayoutUtility.GetLastRect();
                        previewLabelRect.x = _previousLabelRect.x;
                        GUI.Label(previewLabelRect, inputPreviewLabel, _modInputPreviewStyle);
                        
                        var newFieldRect = new Rect(_prevFieldRect) { y = _prevFieldRect.y + _prevFieldRect.height + 2 };
                        float modAmount = modInfluence.floatValue * paramRange;
                        modAmount = modAmount.RoundDigits(3);
                        
                        using (new EditorGUI.DisabledGroupScope(!modulationEnabled.boolValue))
                        {
                            modAmount = EditorGUI.DelayedFloatField(newFieldRect, modAmount);
                            Rect newSliderRect = _previousSliderRect;
                            newSliderRect.y = newFieldRect.y;
                            modAmount = GUI.HorizontalSlider(newSliderRect, modAmount, -paramRange, paramRange);

                            if (!Mathf.Approximately(modInfluence.floatValue, modAmount))
                            {
                                modAmount = Mathf.Clamp(modAmount, -paramRange, paramRange);
                                modInfluence.floatValue = modAmount / paramRange;
                            }
                        }

                        if (modulationEnabled.boolValue)
                        {
                            var valuePreviewLabel = _processedValues[i].Output.RoundDigits(4).ToString();
                            var previewValueRect = new Rect(previewLabelRect) {
                                x = _previousSliderRect.xMax + 5,
                                width = EditorWidth - _previousSliderRect.xMax - 5
                            };
                        
                            GUI.Label(previewValueRect, valuePreviewLabel, _modValuePreviewStyle);
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }
        }

        private void DrawModulationActorHeader()
        {
            EditorGUILayout.Space(2);
            MaxGUI.EditorUILine(_colourDarkGrey);
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Modulation Editor", _titleStyle);
            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            var newActor = (ActorObject)EditorGUILayout.ObjectField(new GUIContent("Test Actor"), Actor, typeof(ActorObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                Actor = newActor;
                ReinitialisePreviewObjects();
            }
        }

        private void DrawModulationToolbar()
        {
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
            if (_selectedModIndex <= -1)
                return;
            
            _modulationHeader = new GUIContent(_parameterProperties[_selectedModIndex].Name);
            _emitterObject.Parameters[_selectedModIndex].IsVolatileEmitter = _isVolatileEmitter;

            if (Actor != null)
                _emitterObject.Parameters[_selectedModIndex].Initialise(Actor);
        }

        private void DrawFadeGroups()
        {
            EditorGUILayout.Space(2);
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
            if (_selectedModIndex <= -1)
            {
                _showModulation = false;
                return;
            }
            
            Parameter.ProcessedValues currentValues = _processedValues[_selectedModIndex];
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Modulation Preview", _titleStyle);
            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("Input", currentValues.Input.RoundDigits(4).ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.Slider(new GUIContent("Normalised"), currentValues.Normalised, 0, 1);
            EditorGUILayout.LabelField("Scaled", currentValues.Scaled.RoundDigits(4).ToString(CultureInfo.InvariantCulture));

            Parameter.ModulationDataObject currentData = _currentParameter.ModulationData;
            
            if (currentData.Accumulate)
                EditorGUILayout.LabelField("Accumulated", currentValues.Accumulated.RoundDigits(4).ToString(CultureInfo.InvariantCulture));
            if (currentData.LimiterMode == ModulationLimiter.Clip)
                EditorGUILayout.LabelField("Raised", currentValues.Raised.RoundDigits(4).ToString(CultureInfo.InvariantCulture));
            if (!currentValues.Instant)
                EditorGUILayout.LabelField("Smoothed", currentValues.Smoothed.RoundDigits(4).ToString(CultureInfo.InvariantCulture));

            float initialOffset = currentData.InitialValue;

            if (_isVolatileEmitter)
            {
                EditorGUILayout.Slider(new GUIContent("Limited"), currentValues.Limited, 0, 1);
            }

            EditorGUILayout.LabelField("Initial Offset",
                initialOffset.RoundDigits(4).ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField(
                "Output", currentValues.Preview.RoundDigits(4).ToString(CultureInfo.InvariantCulture),
                new GUIStyle(EditorStyles.boldLabel));
            
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
            
            _modInputPreviewStyle = new GUIStyle {
                stretchWidth = false,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                normal = new GUIStyleState {
                    textColor = Color.grey
                }
            };
            
            _modValuePreviewStyle = new GUIStyle {
                stretchWidth = false,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                normal = new GUIStyleState {
                    textColor = Color.grey
                }
            };
            
            _toggleOptions = new [] { GUILayout.Height(SmallIconSize), GUILayout.Width(SmallIconSize) };
            _prefixOptions = new[] { GUILayout.Width(PrefixWidth) };
            _paramWithIconOptions = new[] { GUILayout.Width(PrefixWidth - SmallIconSize * 2) };
            _floatFieldOptions = new[] { GUILayout.Width(FloatFieldWidth) };
            
            _previousLabelRect = new Rect(0, 0, 0, 0);
            _prevFieldRect = new Rect(0, 0, 0, 0);
            _previousSliderRect = new Rect(0, 0, 0, 0);
            
            _colourDarkGrey = new Color(0.35f, 0.35f, 0.35f);
            _modulationHeader = GUIContent.none;
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