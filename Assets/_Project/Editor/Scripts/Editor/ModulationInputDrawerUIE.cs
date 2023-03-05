using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


namespace PlaneWaver.Parameters
{
    [CustomPropertyDrawer(typeof(ModulationInputObject))]
    public class ModulationInputDrawerUIE : PropertyDrawer
    {
        private SerializedProperty _inputGroup, _miscInput, _actorInput, _relativeInput, _collisionInput;
        
        // Not complete/implemented
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            _inputGroup = property.FindPropertyRelative("InputGroup");
            _miscInput = property.FindPropertyRelative("MiscInput");
            _actorInput = property.FindPropertyRelative("ActorInput");
            _relativeInput = property.FindPropertyRelative("RelativeInput");
            _collisionInput = property.FindPropertyRelative("CollisionInput");
            
            var container = new VisualElement();
            var label = new Label(property.displayName);
            container.Add(label);

            var groupValue = (InputGroups) _inputGroup.enumValueIndex;
            var miscValue = (InputMisc) _miscInput.enumValueIndex;
            var actorValue = (InputActor) _actorInput.enumValueIndex;
            var relativeValue = (InputRelative) _relativeInput.enumValueIndex;
            var collisionValue = (InputCollision) _collisionInput.enumValueIndex;    
            
            var inputGroup = new EnumField("Input Type", groupValue);
            container.Add(inputGroup);
            return container;
        }

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