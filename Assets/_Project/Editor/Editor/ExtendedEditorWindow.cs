using System;
using UnityEditor;

namespace PlaneWaver.GUI
{
    public class ExtendedEditorWindow : EditorWindow
    {
        protected SerializedObject SerialisedObject;
        protected SerializedProperty CurrentProperty;
        
        protected void DrawProperties(SerializedProperty property, bool drawChildren)
        {
            string lastPropPath = string.Empty;
            
            // foreach (SerializedProperty p in property)
            // {
            //     if (p.isArray && p.propertyType == SerializedPropertyType.Generic)
            //     {
            //         lastPropPath = p.propertyPath;
            //         EditorGUILayout.BeginHorizontal();
            //         p.isExpanded = EditorGUILayout.Foldout(p.isExpanded, p.displayName);
            //         EditorGUILayout.EndHorizontal();
            //
            //         if (p.isExpanded)
            //         {
            //             EditorGUI.indentLevel++;
            //             DrawProperties(p, drawChildren);
            //             EditorGUI.indentLevel--;
            //         }
            //         else
            //         {
            //             if (!string.IsNullOrEmpty(lastPropPath) && p.propertyPath.Contains(lastPropPath))
            //                 continue;
            //             lastPropPath = p.propertyPath;
            //             EditorGUILayout.PropertyField(p, drawChildren);
            //         }
            //     }
            // }
        }
    }
}