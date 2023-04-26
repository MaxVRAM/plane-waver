using UnityEditor;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    public static class EditorList
    {
        public static void Show(SerializedProperty list)
        {
            EditorGUILayout.PropertyField(list);
            EditorGUI.indentLevel += 1;

            if (list.isExpanded)
            {
                for (var i = 0; i < list.arraySize; i++)
                {
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
                }
            }

            EditorGUI.indentLevel -= 1;
        }
    }
}