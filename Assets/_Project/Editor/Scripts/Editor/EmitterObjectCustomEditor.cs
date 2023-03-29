using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using MaxVRAM.CustomGUI;
using MaxVRAM.Extensions;
using PlaneWaver.Library;
using PlaneWaver.Emitters;
using PlaneWaver.Interaction;

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

        private AnimBool[] _editSelectionArray;
        private BaseEmitterObject _emitterObject;
        private SerializedProperty _serialisedParameters;
        private int _parameterCount;

        private ParameterInstance[] _paramInstances;
        private ModulationValues[] _modulationValues;
        private GUIContent[] _parameterIcons;

        private static float EditorWidth => EditorGUIUtility.currentViewWidth - 30;
        private const int PrefixMinWidth = 40;
        private const int PrefixMaxWidth = 140;
        private const int PrefixWidth = 140;
        private const int SmallIconSize = 18;
        private const int FloatFieldWidth = 40;
        private const int ToolbarIconSize = 48;
        private float _modulationHeaderHalfWidth;
        private float _parameterSliderWidth;

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

        // TODO - Add separate section for noise modulation with its own enable/disable toggle.

        /// <summary>
        /// Define GUI styles and options for the Emitter Object when the inspector is enabled.
        /// </summary>
        public void OnEnable()
        {
            EditorApplication.update += Update;
            EditorApplication.pauseStateChanged += HandlePauseState;
            _selectedModIndex = -1;

            _emitterObject = (BaseEmitterObject)target;
            _parameterCount = _emitterObject.Parameters.Count;
            _isVolatileEmitter = _emitterObject is VolatileEmitterObject;
            _serialisedParameters = serializedObject.FindProperty("Parameters.Array");
            _paramInstances = new ParameterInstance[_parameterCount];
            _modulationValues = new ModulationValues[_parameterCount];
            _parameterIcons = new GUIContent[_parameterCount];
            _editSelectionArray = new AnimBool[_parameterCount];

            for (var i = 0; i < _parameterCount; i++)
            {
                _paramInstances[i] = new ParameterInstance(_emitterObject.Parameters[i]);
                _modulationValues[i] = _paramInstances[i].Values;
                _parameterIcons[i] = _emitterObject.Parameters[i].GetGUIContent();
                _editSelectionArray[i] = new AnimBool(false);
                _editSelectionArray[i].valueChanged.AddListener(Repaint);
            }

            DefineStyles();
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
                Actor.OnNewValidCollision += TriggerCollisionEmitters;

            _actorSet = Actor != null;

            if (!_actorSet || !Application.isPlaying || _isPaused)
                return;

            for (var i = 0; i < _parameterCount; i++)
            {
                if (_isVolatileEmitter && _modulationValues[i].Instant && !_volatileTriggered)
                    continue;

                _paramInstances[i].UpdateInputValue(Actor);
                _modulationValues[i].Process();
            }

            if (_selectedModIndex >= 0 && _selectedModIndex < _parameterCount)
            {
                _showModulation = _paramInstances[_selectedModIndex].ParameterRef.Input.Enabled;
            }
            else
            {
                _showModulation = false;
                _selectedModIndex = -1;
            }
            
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
            DrawModulationActorSection();
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

            var bgColor = new GUIStyle {
                normal = {
                    background = Texture2D.grayTexture
                }
            };
            var audioObject = (AudioObject)serialisedAudioObject.objectReferenceValue;

            if (audioObject == null) return;

            Editor audioAssetEditor = CreateEditor(audioObject.Clip);
            audioAssetEditor.OnInteractivePreviewGUI
                    (GUILayoutUtility.GetRect(EditorWidth, audioObjectWidth), bgColor);
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
                for (var i = 0; i < _serialisedParameters.arraySize; i++)
                {
                    SerializedProperty currentParam = _serialisedParameters.GetArrayElementAtIndex(i);
                    SerializedProperty inputProperty = currentParam.FindPropertyRelative("Input");
                    SerializedProperty outputProperty = currentParam.FindPropertyRelative("Output");
                    SerializedProperty noiseProperty = currentParam.FindPropertyRelative("Noise");

                    string paramName = currentParam.FindPropertyRelative("ParameterName").stringValue;
                    bool isLength = currentParam.FindPropertyRelative("ParamType").enumValueIndex ==
                                    (int)ParameterType.Length;
                    Vector2 paramRangeVector = currentParam.FindPropertyRelative("Range").vector2Value;
                    float paramRange = paramRangeVector.y - paramRangeVector.x;
                    SerializedProperty baseRange = currentParam.FindPropertyRelative("BaseRange");
                    SerializedProperty enabled = inputProperty.FindPropertyRelative("Enabled");


                    EditorGUILayout.BeginVertical();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(_parameterIcons[i], _toggleOptions);
                        EditorGUILayout.LabelField(paramName, _paramWithIconOptions);
                        _previousLabelRect = GUILayoutUtility.GetLastRect();

                        GUIContent modIcon = enabled.boolValue
                                ? IconManager.ToggleIcons["ModulationOn"]
                                : IconManager.ToggleIcons["ModulationOff"];

                        if (GUILayout.Button(modIcon, _toggleStyle, _toggleOptions))
                            enabled.boolValue = !enabled.boolValue;

                        Vector2 initRange = baseRange.vector2Value;
                        initRange.x = initRange.x.LimitDigits(4);
                        initRange.y = initRange.y.LimitDigits(4);
                        initRange.x = EditorGUILayout.DelayedFloatField(initRange.x, _floatFieldOptions);
                        _prevFieldRect = GUILayoutUtility.GetLastRect();

                        if (!isLength)
                        {
                            EditorGUILayout.MinMaxSlider
                            (ref initRange.x, ref initRange.y, paramRangeVector.x,
                                paramRangeVector.y);
                            _previousSliderRect = GUILayoutUtility.GetLastRect();
                            if (_previousSliderRect.width > 1)
                                _parameterSliderWidth = _previousSliderRect.width;
                        }
                        else
                        {
                            initRange.x = GUILayout.HorizontalSlider
                            (initRange.x, paramRangeVector.x, paramRangeVector.y,
                                GUILayout.Width(_parameterSliderWidth));
                            _previousSliderRect = GUILayoutUtility.GetLastRect();
                        }

                        if (!isLength)
                            initRange.y = EditorGUILayout.DelayedFloatField(initRange.y, _floatFieldOptions);

                        baseRange.vector2Value = initRange;

                        if (_isVolatileEmitter && !isLength)
                        {
                            SerializedProperty reversePath = currentParam.FindPropertyRelative("ReversePath");
                            GUIContent revIcon = reversePath.boolValue
                                    ? IconManager.ToggleIcons["PathReverse"]
                                    : IconManager.ToggleIcons["PathForward"];

                            if (GUILayout.Button(revIcon, _toggleStyle, _toggleOptions))
                                reversePath.boolValue = !reversePath.boolValue;
                        }
                    }


                    EditorGUILayout.Space(2);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string inputName = enabled.boolValue ? _paramInstances[i].ParameterRef.Input.Source.GetInputName() : " - ";

                        SerializedProperty outputAmount = outputProperty.FindPropertyRelative("Amount");
                        EditorGUILayout.LabelField(GUIContent.none, _paramWithIconOptions);
                        Rect previewLabelRect = GUILayoutUtility.GetLastRect();
                        previewLabelRect.x = _previousLabelRect.x;
                        GUI.Label(previewLabelRect, inputName, _modInputPreviewStyle);

                        var newFieldRect = new Rect(_prevFieldRect) {
                            y = _prevFieldRect.y + _prevFieldRect.height + 2
                        };
                        float modAmount = outputAmount.floatValue * paramRange;
                        modAmount = modAmount.LimitDigits(3);

                        using (new EditorGUI.DisabledGroupScope(!enabled.boolValue))
                        {
                            EditorGUI.BeginChangeCheck();
                            modAmount = EditorGUI.DelayedFloatField(newFieldRect, modAmount);
                            Rect newSliderRect = _previousSliderRect;
                            newSliderRect.y = newFieldRect.y;
                            modAmount = GUI.HorizontalSlider
                                    (newSliderRect, modAmount, -paramRange, paramRange);

                            if (EditorGUI.EndChangeCheck())
                            {
                                modAmount = Mathf.Clamp(modAmount, -paramRange, paramRange);
                                outputAmount.floatValue = modAmount / paramRange;
                            }
                        }

                        if (enabled.boolValue)
                        {
                            var valuePreviewLabel = _paramInstances[i].Values.Output.LimitDigits
                                    (4).ToString();
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

        private void DrawModulationActorSection()
        {
            EditorGUILayout.Space(2);
            MaxGUI.EditorUILine(_colourDarkGrey);
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Modulation Editor", _titleStyle);
            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            var newActor = (ActorObject)EditorGUILayout.ObjectField
                    (new GUIContent("Test Actor"), Actor, typeof(ActorObject), true);

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
                _toolbarOptions = new[] {
                    GUILayout.MaxHeight(ToolbarIconSize),
                    GUILayout.Width(EditorGUIUtility.currentViewWidth - 30)
                };

                int prevIndex = _selectedModIndex;
                EditorGUI.BeginChangeCheck();
                int newIndex = UpdateSelectedModulationToggles
                (GUILayout.Toolbar
                        (_selectedModIndex, _parameterIcons, toolbarStyle, _toolbarOptions));
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
                x = toolbarRect.x +
                    toolbarItemWidth * _selectedModIndex +
                    toolbarItemHalf -
                    _modulationHeaderHalfWidth,
                y = toolbarRect.y + toolbarRect.height + 4
            };
            GUI.Label(modulationHeaderRect, _modulationHeader, _modulationTypeHeader);
        }

        private void ReinitialisePreviewObjects()
        {
            if (_selectedModIndex <= -1)
                return;

            _modulationHeader = new GUIContent(_paramInstances[_selectedModIndex].ParameterRef.ParameterName);
        }

        private void DrawFadeGroups()
        {
            EditorGUILayout.Space(2);

            // Dynamically display modulation editor for selected parameter
            for (var i = 0; i < _editSelectionArray.Length; i++)
            {
                if (EditorGUILayout.BeginFadeGroup(_editSelectionArray[i].faded))
                    EditorGUILayout.PropertyField(_serialisedParameters.GetArrayElementAtIndex(i));
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

            Parameter currentParameter = _paramInstances[_selectedModIndex].ParameterRef;
            ModulationValues currentValues = _paramInstances[_selectedModIndex].Values;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Modulation Preview", _titleStyle);
            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField
                    ("Input", currentValues.Input.LimitDigits(4).ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.Slider(new GUIContent("Normalised"), currentValues.Normalised, 0, 1);
            EditorGUILayout.LabelField
                    ("Scaled", currentValues.Scaled.LimitDigits(4).ToString(CultureInfo.InvariantCulture));

            if (currentParameter.Input.Accumulate)
                EditorGUILayout.LabelField
                ("Accumulated",
                    currentValues.Accumulated.LimitDigits(4).ToString(CultureInfo.InvariantCulture));
            if (currentParameter.Output.Limiter == ModulationLimiter.Clip)
                EditorGUILayout.LabelField
                ("Raised",
                    currentValues.Raised.LimitDigits(4).ToString(CultureInfo.InvariantCulture));
            if (!currentValues.Instant)
                EditorGUILayout.LabelField
                ("Smoothed",
                    currentValues.Smoothed.LimitDigits(4).ToString(CultureInfo.InvariantCulture));

            float initialOffset = currentValues.Initial;

            if (_isVolatileEmitter)
            {
                EditorGUILayout.Slider(new GUIContent("Limited"), currentValues.Limited, 0, 1);
            }

            EditorGUILayout.LabelField
                    ("Initial Offset", initialOffset.LimitDigits(4).ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField
            ("Output", currentValues.Preview.LimitDigits(4).ToString(CultureInfo.InvariantCulture),
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
                margin = new RectOffset(0, 0, 2, 0),
                padding = new RectOffset(1, 1, 1, 1),
                border = new RectOffset(1, 1, 1, 1),
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

            _toggleOptions = new[] { GUILayout.Height(SmallIconSize), GUILayout.Width(SmallIconSize) };
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