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

        private Texture _modulationOnIcon;
        private Texture _modulationOffIcon;
        private Texture _volatileForwardIcon;
        private Texture _volatileBackIcon;
        
        private const int PrefixWidth = 30;
        private const int ToggleWidth = 30;
        private const int FloatWidth = 40;
        private GUILayoutOption _prefixWidthOption;
        private GUILayoutOption _toggleLabelWidth;
        private GUILayoutOption _floatFieldWidth;
        
        private const int IconSize = 32;
        private const string IconPath = "Assets/_Project/Resources/Icons/";
        private static Texture[] _parameterIcons;

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
            
            _parameterProperties = ((BaseEmitterObject)target)
                                  .Parameters.ConvertAll(parameter => parameter.ParameterProperties)
                                  .ToArray();
            
            _parameterIcons = new Texture[_parameterArray.arraySize];
            
            foreach (PropertiesObject parameter in _parameterProperties)
                _parameterIcons[parameter.Index] = EditorGUIUtility.Load(IconPath + parameter.Icon) as Texture;
            
            //_parameterNames = GetParameterNameArray(_parameterArray);
            _enabledParameters = new bool[_parameterArray.arraySize];
            
            _titleStyle = new GUIStyle {
                fontSize = 16,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState {
                    textColor = Color.white
                }
            };
            
            _modulationOnIcon = EditorGUIUtility.Load( IconPath + "icon.pulse.png") as Texture;
            _modulationOffIcon = EditorGUIUtility.Load(IconPath + "icon.trending-neutral.png") as Texture;
            _volatileBackIcon = EditorGUIUtility.Load(IconPath + "icon.arrow-left-thin-circle-outline.png") as Texture;
            _volatileForwardIcon = EditorGUIUtility.Load(IconPath + "icon.arrow-right-thin-circle-outline.png") as Texture;

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
                
                SerializedProperty modulationEnabled = modulationData.FindPropertyRelative("ModulationEnabled");
                Texture modIcon = modulationEnabled.boolValue ? _modulationOnIcon : _modulationOffIcon;
                Texture invIcon = modulationEnabled.boolValue ? _volatileBackIcon : _volatileForwardIcon;

                var buttonStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button) {
                    fixedWidth = IconSize * 0.5f,
                    fixedHeight = IconSize * 0.5f,
                    stretchWidth = true,
                    stretchHeight = true
                };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(_parameterProperties[i].Name);
                EditorGUILayout.EditorToolbar();
                
                if (_isVolatile)
                {
                    EditorGUI.BeginChangeCheck();
                    SerializedProperty invertVolatileRange = modulationData.FindPropertyRelative("InvertVolatileRange");
                    int invIndex = invertVolatileRange.boolValue ? 1 : 0;
                    int prevIndex = invIndex;
                    invertVolatileRange.boolValue = GUILayout.Toolbar(invIndex, new[] { invIcon }, buttonStyle) == 1;
                    // TODO - fix this
                    if (EditorGUI.EndChangeCheck()) invertVolatileRange.boolValue = !invertVolatileRange.boolValue;
                    //invertVolatileRange.boolValue = GUILayout.Toggle(invertVolatileRange.boolValue, invIcon, _toggleLabelWidth);
                    //invertVolatileRange.boolValue = EditorGUILayout.Toggle(invertVolatileRange.boolValue, _modulationOffIcon);
                }
                
                modulationEnabled.boolValue = GUILayout.Toggle(modulationEnabled.boolValue, modIcon, _toggleLabelWidth);
                //modulationEnabled.boolValue = EditorGUILayout.Toggle(modulationEnabled.boolValue, _toggleLabelWidth);
                _enabledParameters[i] = modulationEnabled.boolValue;
                
                Vector2 initRange = initialRange.vector2Value;
                initRange.x = initRange.x.RoundDigits(4);
                initRange.y = initRange.y.RoundDigits(4);
                initRange.x = EditorGUILayout.DelayedFloatField(initRange.x, _floatFieldWidth);
                
                EditorGUILayout.MinMaxSlider
                        (ref initRange.x, ref initRange.y, 
                            _parameterProperties[i].ParameterRange.x, 
                            _parameterProperties[i].ParameterRange.y);
                
                initRange.y = EditorGUILayout.DelayedFloatField(initRange.y, _floatFieldWidth);
                initialRange.vector2Value = initRange;
                
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();


            EditorGUILayout.Space(20);
            // EditorGUILayout.LabelField("Modulation Editor", _titleStyle);
            // EditorGUILayout.Space(10);
            //  ChangeDisplayedParameter(GUILayout.SelectionGrid(_selectedModIndex, _parameterNames, 3));

            EditorGUILayout.LabelField("Modulation Editor", _titleStyle);
            
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
                int newIndex = UpdateSelectedModulationToggles(GUILayout.Toolbar(_selectedModIndex, _parameterIcons, toolbarStyle));
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

        // private string GetParameterName(SerializedProperty parameter)
        // {
        //     return parameter.FindPropertyRelative("ParameterProperties").FindPropertyRelative("Name").stringValue;
        // }
        //
        // private string[] GetParameterNameArray(SerializedProperty parameterArray)
        // {
        //     var parameterNames = new string[parameterArray.arraySize];
        //     for (var i = 0; i < parameterNames.Length; i++)
        //         parameterNames[i] = _parameterArray.GetArrayElementAtIndex(i).FindPropertyRelative
        //                 ("ParameterProperties").FindPropertyRelative("Name").stringValue;
        //     return parameterNames;
        // }
        //
        // public void GetParameterDetails(SerializedProperty paramArray, out string[] paramNames, out Texture2D[] paramIcons)
        // {
        //     paramNames = new string[paramArray.arraySize];
        //     paramIcons = new Texture2D[paramArray.arraySize];
        //
        //     for (var i = 0; i < paramArray.arraySize; i++)
        //     {
        //         SerializedProperty paramProps = paramArray.GetArrayElementAtIndex(i).FindPropertyRelative("ParameterProperties");
        //         paramNames[i] = paramProps.FindPropertyRelative("Name").stringValue;
        //         paramIcons[i] = EditorGUIUtility.Load(IconPath + paramProps.FindPropertyRelative("Icon").stringValue) as Texture2D;
        //     }
        // }
        
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