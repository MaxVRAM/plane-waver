using UnityEditor;
using UnityEngine;


namespace PlaneWaver.Modulation
{
    [CustomPropertyDrawer(typeof(ModulationInputObject))]
    public class ModulationInputDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int indent = EditorGUI.indentLevel;
            
            SerializedProperty inputGroup = property.FindPropertyRelative("InputGroup");
            SerializedProperty selectedGroup = GetGroup(inputGroup, property);
            
            label = new GUIContent("Modulation Source");
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.indentLevel = 0;
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            var groupWidth = (int)(position.width * 0.4f);
            var itemWidth = (int)(position.width - groupWidth);
            var groupRect = new Rect(position.x, position.y, groupWidth, position.height);
            var itemRect = new Rect(position.x + groupWidth, position.y, itemWidth, position.height);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(groupRect, inputGroup, GUIContent.none);
            if (EditorGUI.EndChangeCheck()) { selectedGroup = GetGroup(inputGroup, property); }
            
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