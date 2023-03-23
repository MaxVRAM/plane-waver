using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace MaxVRAM.CustomGUI
{
    public static class MaxGUI
    {
        /// <summary>
        /// Draw a horizontal line in the editor.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="thickness"></param>
        /// <param name="padding"></param>
        public static void EditorUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding * 0.5f;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
        
        /// <summary>
        /// Create a GUILayout.Width option from a GUIContent or string matching the exact content width.
        /// </summary>
        public static GUILayoutOption ExactWidthOption(string label)
        {
            return GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent(label)).x);
        }
        /// <summary>
        /// Create a GUILayout.Width option from a GUIContent or string matching the exact content width.
        /// </summary>
        public static GUILayoutOption ExactWidthOption(GUIContent content)
        {
            return GUILayout.Width(GUI.skin.label.CalcSize(content).x);
        }
        
        /// <summary>
        /// Add a fixed width to the input style matching the exact string width.
        /// </summary>
        public static GUIStyle ExactWidthStyle(string label, GUIStyle style)
        {
            style.fixedWidth = GUI.skin.label.CalcSize(new GUIContent(label)).x;
            return style;
        }
        /// <summary>
        /// Create a GUILayout.Width option from a GUIContent or string matching the exact content width.
        /// </summary>
        public static GUIStyle ExactWidthStyle(GUIContent content, GUIStyle style)
        {
            style.fixedWidth = GUI.skin.label.CalcSize(content).x;
            return style;
        }
    }
}