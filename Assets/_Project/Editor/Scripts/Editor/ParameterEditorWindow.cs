using UnityEditor;

namespace PlaneWaver.Parameters
{
    public class ParameterEditorWindow : EditorWindow
    {
        public static void Open(Parameter parameter)
        {
            var window = GetWindow<ParameterEditorWindow>("Parameter Editor");
        }
    }
}