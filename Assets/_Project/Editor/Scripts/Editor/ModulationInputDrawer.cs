using UnityEditor;
using UnityEngine;


namespace PlaneWaver.Modulation
{
    [CustomPropertyDrawer(typeof(ModulationInputObject))]
    public class ModulationInputDrawer : PropertyDrawer
    {
        private SerializedProperty _inputGroup, _miscInput, _actorInput, _relativeInput, _collisionInput;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int indent = EditorGUI.indentLevel;
            _inputGroup = property.FindPropertyRelative("InputGroup");
            SerializedProperty selectedGroup = GetGroup(_inputGroup, property);
            
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.indentLevel = 0;
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var groupRect = new Rect(position.x, position.y, 100, position.height);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(groupRect, _inputGroup, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                selectedGroup = GetGroup(_inputGroup, property);
            }
            var itemRect = new Rect(position.x + 100, position.y, position.width - 100, position.height);
            EditorGUI.PropertyField(itemRect, selectedGroup, GUIContent.none);
            
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static SerializedProperty GetGroup(SerializedProperty group, SerializedProperty property)
        {
            var input = (InputGroups) group.enumValueIndex;
            return property.FindPropertyRelative(input.ToString());
        }   
    }
}