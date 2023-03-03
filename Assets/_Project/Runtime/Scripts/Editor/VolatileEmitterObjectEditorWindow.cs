using PlaneWaver.Emitters;
using UnityEditor;

namespace PlaneWaver.Emitters
{
    public class VolatileEmitterObjectEditorWindow : EditorWindow
    {
        [MenuItem("Window/Volatile Emitter Editor")]
        public static void ShowWindow()
        {
            GetWindow<VolatileEmitterObjectEditorWindow>("Volatile Emitter Editor");
        }
        
        public static void Open(VolatileEmitterObject emitterObject)
        {
            var window = GetWindow<VolatileEmitterObjectEditorWindow>("Volatile Emitter Editor");
            
            // var window = GetWindow<EmitterObjectEditorWindow>("Emitter Object Editor");
            // window.emitterObject = emitterObject;
        }
        private void OnGUI() { }
    }
}