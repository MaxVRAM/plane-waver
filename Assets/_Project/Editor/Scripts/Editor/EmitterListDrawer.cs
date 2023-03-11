using System;
using UnityEditor;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    [CustomPropertyDrawer(typeof(EmitterListWrapper<>))]
    internal class ListWrapperDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty listProperty = property.FindPropertyRelative("List");
            
            foreach (SerializedProperty list in listProperty)
            {
                list.objectReferenceValue = EditorGUI.ObjectField(position,
                    list.objectReferenceValue, typeof(EmitterAuth), true);
            }
            
            //Do your own thing here. If you need to get the list type, you can do:
            Type type = fieldInfo.FieldType.GetGenericArguments()[0];
        }
    }
}