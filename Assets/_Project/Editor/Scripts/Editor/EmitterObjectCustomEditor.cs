using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using PlaneWaver.Emitters;
using UnityEditor.AnimatedValues;

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

        // private EmitterObject _emitterObject;
        // private Parameter[] _parameterObjects;
        // private SerializedProperty[] _parameterProperties;

        public void OnEnable()
        {
            _emitterName = serializedObject.FindProperty("EmitterName");
            _description = serializedObject.FindProperty("Description");
            _audioAsset = serializedObject.FindProperty("AudioObject");
            _parameterArray = serializedObject.FindProperty("Parameters.Array");
            _parameterNames = GetParameterNameArray(_parameterArray);
            
            _isVolatile = (BaseEmitterObject)target is VolatileEmitterObject;

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

            // _emitterObject.Description = EditorGUILayout.TextField("Description", _emitterObject.Description);
            //_emitterObject.AudioObject = (AudioObject)EditorGUILayout.ObjectField(_emitterObject.AudioObject, typeof(AudioObject),false);

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < _parameterToggles.Length; i++)
            {
                UnityEngine.GUI.enabled = _selectedParameterIndex != i;
                if (GUILayout.Button(_parameterNames[i])) ChangeDisplayedParameter(i);
                UnityEngine.GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            // SerializedProperty parameter = _parameterArray.GetArrayElementAtIndex(i);
            // SerializedProperty parameterProperties = parameter.FindPropertyRelative("ParameterProperties");
            // WORKS
            // EditorGUILayout.PropertyField(parameter.FindPropertyRelative("ModulationInput"));
            // EditorGUILayout.PropertyField(parameter.FindPropertyRelative("ParameterProperties"));
            // EditorGUILayout.Space();
            
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
        }
        
        private string[] GetParameterNameArray(SerializedProperty parameterArray)
        {
            var parameterNames = new string[parameterArray.arraySize];
            for (var i = 0; i < parameterNames.Length; i++)
            {
                SerializedProperty parameter = _parameterArray.GetArrayElementAtIndex(i);
                SerializedProperty parameterProperties = parameter.FindPropertyRelative("ParameterProperties");
                SerializedProperty parameterName = parameterProperties.FindPropertyRelative("Name");
                parameterNames[i] = parameterName.stringValue;
            }

            return parameterNames;
        }

        private void ChangeDisplayedParameter(int index)
        {
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