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
        private int _selectedModIndex;
        private AnimBool[] _parameterToggles;
        
        private BaseEmitterObject _emitterObject;

        private SerializedProperty _emitterName;
        private SerializedProperty _description;
        private SerializedProperty _audioAsset;
        private SerializedProperty _parameterArray;
        
        private PropertiesObject[] _parameterProperties;
        
        private string[] _parameterNames;
        private bool[] _enabledParameters;

        // Unity Docs GUIStyle EditorStyles reference
        // Ref: https://docs.unity3d.com/2021.3/Documentation/ScriptReference/EditorStyles.html
        private static GUIStyle _titleStyle;
        private static GUIStyle _toolbarStyle;

        private GUIContent _modulationOnIcon;
        private GUIContent _modulationOffIcon;
        private GUIContent _volatileForwardIcon;
        private GUIContent _volatileReverseIcon;
        
        private const int PrefixWidth = 30;
        private const int ToggleWidth = 30;
        private const int FloatWidth = 40;
        private GUILayoutOption _prefixWidthOption;
        private GUILayoutOption _toggleLabelWidth;
        private GUILayoutOption _floatFieldWidth;
        
        private const int ToggleSize = 24;
        private const int IconSize = 64;
        private const string IconPath = "Assets/_Project/Resources/Icons/";
        private GUIContent[] _parameterIcons;
        
        public void OnEnable()
        {
            _emitterObject = (BaseEmitterObject)target;
            _isVolatile = _emitterObject is VolatileEmitterObject;
            _emitterName = serializedObject.FindProperty("EmitterName");
            _description = serializedObject.FindProperty("Description");
            _audioAsset = serializedObject.FindProperty("AudioObject");
            _parameterArray = serializedObject.FindProperty("Parameters.Array");
            
            _parameterProperties = _emitterObject.Parameters.ConvertAll(parameter => parameter.ParameterProperties).ToArray();
            
            _parameterIcons = new GUIContent[_parameterArray.arraySize];

            // TODO - This is coming up null for some reason - need to investigate
            for (var i = 0; i < _parameterArray.arraySize; i++)
                _parameterIcons[i] = _emitterObject.Parameters[i].GetIcon();

            _enabledParameters = new bool[_parameterArray.arraySize];
            
            _titleStyle = new GUIStyle {
                fontSize = 16,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState {
                    textColor = Color.white
                }
            };
            
            _modulationOnIcon = IconManager.Instance.ModulationIcons.On;
            _modulationOffIcon = IconManager.Instance.ModulationIcons.Off;
            _volatileForwardIcon = IconManager.Instance.ModulationIcons.Forward;
            _volatileReverseIcon = IconManager.Instance.ModulationIcons.Reverse;

            _prefixWidthOption = GUILayout.Width(PrefixWidth);
            _toggleLabelWidth = GUILayout.Width(ToggleWidth);
            _floatFieldWidth = GUILayout.Width(FloatWidth);
            
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
            
            EditorGUILayout.PropertyField(_emitterName);
            EditorGUILayout.PropertyField(_description);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_audioAsset);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            
            for (var i = 0; i < _parameterArray.arraySize; i++)
            {
                SerializedProperty currentParam = _parameterArray.GetArrayElementAtIndex(i);
                SerializedProperty modulationData = currentParam.FindPropertyRelative("ModulationData");
                SerializedProperty initialRange = modulationData.FindPropertyRelative("InitialRange");
                
                var buttonStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button) {
                    fixedWidth = ToggleSize,
                    fixedHeight = ToggleSize,
                    stretchWidth = true,
                    stretchHeight = true
                };

                var boxStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).box) {
                    fixedWidth = ToggleSize,
                    fixedHeight = ToggleSize,
                    stretchWidth = true,
                    stretchHeight = true,
                    alignment = TextAnchor.MiddleCenter
                };
                
                GUILayoutOption[] toggleOptions = {
                    GUILayout.Height(ToggleSize),
                    GUILayout.Width(ToggleSize)
                };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(_parameterProperties[i].Name);
                
                SerializedProperty modulationEnabled = modulationData.FindPropertyRelative("ModulationEnabled");
                GUIContent modIcon = modulationEnabled.boolValue ? _modulationOnIcon : _modulationOffIcon;
                
                if (GUILayout.Button(modIcon, toggleOptions))
                    modulationEnabled.boolValue = !modulationEnabled.boolValue;
                
                Vector2 initRange = initialRange.vector2Value;
                initRange.x = initRange.x.RoundDigits(4);
                initRange.y = initRange.y.RoundDigits(4);
                initRange.x = EditorGUILayout.DelayedFloatField(initRange.x, _floatFieldWidth);
                
                EditorGUILayout.MinMaxSlider(
                    ref initRange.x, ref initRange.y, 
                    _parameterProperties[i].ParameterRange.x, 
                    _parameterProperties[i].ParameterRange.y
                );
                
                initRange.y = EditorGUILayout.DelayedFloatField(initRange.y, _floatFieldWidth);
                initialRange.vector2Value = initRange;
                
                if (_isVolatile)
                {
                    SerializedProperty reversePath = modulationData.FindPropertyRelative("ReversePath");
                    GUIContent revIcon = reversePath.boolValue ? _modulationOnIcon : _modulationOffIcon;
                
                    if (GUILayout.Button(revIcon, toggleOptions))
                        reversePath.boolValue = !reversePath.boolValue;
                    
                    // EditorGUI.BeginChangeCheck();
                    // SerializedProperty invertVolatileRange = modulationData.FindPropertyRelative("ReversePath");
                    // GUIContent invIcon = invertVolatileRange.boolValue ? _volatileReverseIcon : _volatileForwardIcon;
                    // int nowInv = invertVolatileRange.boolValue ? 0 : -1;
                    // int newInv = GUILayout.SelectionGrid(nowInv, new[] { invIcon }, 1, buttonStyle);
                    // if (EditorGUI.EndChangeCheck()) newInv = newInv == nowInv ? -1 : newInv;
                    // invertVolatileRange.boolValue = newInv == 0;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Modulation Editor", _titleStyle);

            GUILayoutOption[] toolbarOptions = {
                GUILayout.Height(IconSize),
                GUILayout.ExpandWidth(true)
            };
            
            var toolbarStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button) {
                fixedWidth = IconSize,
                fixedHeight = IconSize,
                stretchWidth = true,
                stretchHeight = true
            };
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                int prevIndex = _selectedModIndex;
                int newIndex = UpdateSelectedModulationToggles(GUILayout.Toolbar(_selectedModIndex, _parameterIcons, toolbarStyle, toolbarOptions));
                if (EditorGUI.EndChangeCheck()) newIndex = newIndex == prevIndex ? -1 : newIndex;
                _selectedModIndex = UpdateSelectedModulationToggles(newIndex);
            }
            
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