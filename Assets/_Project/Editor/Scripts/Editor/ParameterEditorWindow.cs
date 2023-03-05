using UnityEditor;

namespace PlaneWaver.Modulation
{
    public class ParameterEditorWindow : EditorWindow
    {
        public static void Open(Parameter parameter)
        {
            var window = GetWindow<ParameterEditorWindow>("Parameter Editor");
        }
    }
}